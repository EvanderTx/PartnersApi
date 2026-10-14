using System.Net;
using Partners.Application.Partners.Dtos;
using Partners.Domain.Partners;
using Partners.Tests.Infrastructure;

namespace Partners.Tests.Api;

public sealed class UpdatePartnerTests(PartnersApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Update_ChangesDataAndVersion_ButKeepsCreatedAt()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");
        var body = UpdateBodyFrom(partner);
        body["name"] = "  Ana Maria Silva ";
        body["type"] = "BOTH";
        body["email"] = "ana@example.com";
        body["creditLimit"] = 2500.75m;

        var response = await PutAsync(partner.Id, body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ReadAsync<PartnerResponse>(response);
        Assert.Equal("Ana Maria Silva", updated.Name);
        Assert.Equal(PartnerType.Both, updated.Type);
        Assert.Equal("ana@example.com", updated.Email);
        Assert.Equal(2500.75m, updated.CreditLimit);
        Assert.Equal(partner.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= partner.UpdatedAt);
        Assert.NotEqual(partner.Version, updated.Version);
        Assert.Equal(updated, await GetAsync(partner.Id));
    }

    // Scenario 3
    [Fact]
    public async Task DeactivateAndReactivate_ControlsDefaultListing()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");

        var deactivateBody = UpdateBodyFrom(partner);
        deactivateBody["isActive"] = false;
        var deactivated = await ReadAsync<PartnerResponse>(await PutAsync(partner.Id, deactivateBody));

        Assert.False(deactivated.IsActive);
        Assert.Empty((await ListAsync()).Items);
        Assert.Contains((await ListAsync("?IncludeInactive=true")).Items, p => p.Id == partner.Id);
        Assert.False((await GetAsync(partner.Id)).IsActive);

        var reactivateBody = UpdateBodyFrom(deactivated);
        reactivateBody["isActive"] = true;
        var reactivated = await PutAsync(partner.Id, reactivateBody);

        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
        Assert.Contains((await ListAsync()).Items, p => p.Id == partner.Id);
    }

    // Scenario 4
    [Fact]
    public async Task Update_KeepingOwnTaxNumber_IsAllowed()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");
        var body = UpdateBodyFrom(partner);
        body["taxNumber"] = "123.456.789";
        body["name"] = "Ana Silva Lda";

        var response = await PutAsync(partner.Id, body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("123456789", (await ReadAsync<PartnerResponse>(response)).TaxNumber);
    }

    // Scenario 4
    [Fact]
    public async Task Update_ToTaxNumberOfAnotherPartner_ReturnsConflictAndKeepsData()
    {
        var ana = await CreatePartnerAsync("Ana Silva", "111111111");
        await CreatePartnerAsync("Bruno Costa", "222222222");
        var body = UpdateBodyFrom(ana);
        body["name"] = "Nome Alterado";
        body["taxNumber"] = "222-222-222";

        var response = await PutAsync(ana.Id, body);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal(ana, await GetAsync(ana.Id));
    }

    // Scenario 6
    [Theory]
    [InlineData("not-base64!")]
    [InlineData("AAAA")]
    [InlineData("")]
    public async Task Update_WithMalformedVersion_ReturnsBadRequestWithoutChanges(string version)
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");
        var body = UpdateBodyFrom(partner);
        body["name"] = "Nome Alterado";
        body["version"] = version;

        var response = await PutAsync(partner.Id, body);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("Version", out _));
        Assert.Equal(partner, await GetAsync(partner.Id));
    }

    [Fact]
    public async Task Update_WithInvalidField_DoesNotChangeAnyField()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");
        var body = UpdateBodyFrom(partner);
        body["name"] = "Nome Alterado";
        body["creditLimit"] = 10.999m;

        var response = await PutAsync(partner.Id, body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(partner, await GetAsync(partner.Id));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("taxNumber")]
    [InlineData("type")]
    [InlineData("email")]
    [InlineData("creditLimit")]
    [InlineData("isActive")]
    [InlineData("version")]
    public async Task Update_WithMissingField_ReturnsBadRequest(string missingField)
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");
        var body = UpdateBodyFrom(partner);
        body.Remove(missingField);

        var response = await PutAsync(partner.Id, body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(partner, await GetAsync(partner.Id));
    }

    [Fact]
    public async Task Update_WithNullEmail_ClearsEmail()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789", email: "ana@example.com");
        var body = UpdateBodyFrom(partner);
        body["email"] = null;

        var response = await PutAsync(partner.Id, body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await ReadAsync<PartnerResponse>(response)).Email);
    }

    [Fact]
    public async Task Update_WithStaleVersion_ReturnsConflictAndKeepsData()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");
        var firstBody = UpdateBodyFrom(partner);
        firstBody["name"] = "Primeira Alteracao";
        Assert.Equal(HttpStatusCode.OK, (await PutAsync(partner.Id, firstBody)).StatusCode);

        var staleBody = UpdateBodyFrom(partner);
        staleBody["name"] = "Alteracao Antiga";
        var response = await PutAsync(partner.Id, staleBody);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Primeira Alteracao", (await GetAsync(partner.Id)).Name);
    }

    [Fact]
    public async Task Update_WhenPartnerDoesNotExist_ReturnsNotFound()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");

        var response = await PutAsync(Guid.NewGuid(), UpdateBodyFrom(partner));

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
