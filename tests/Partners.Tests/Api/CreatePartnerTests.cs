using System.Net;
using System.Net.Http.Json;
using Partners.Application.Partners.Dtos;
using Partners.Domain.Partners;
using Partners.Tests.Infrastructure;

namespace Partners.Tests.Api;

public sealed class CreatePartnerTests(PartnersApiFactory factory) : ApiTestBase(factory)
{
    // Scenario 1
    [Fact]
    public async Task Create_TrimsNameAndNormalizesTaxNumber()
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner(" Ana Silva ", "001.234.567"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<PartnerResponse>(response);
        Assert.Equal("Ana Silva", created.Name);
        Assert.Equal("001234567", created.TaxNumber);
        Assert.Equal(PartnerType.Customer, created.Type);
        Assert.True(created.IsActive);
        Assert.False(string.IsNullOrEmpty(created.Version));
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.Equal(created.CreatedAt, created.UpdatedAt);
        Assert.Equal($"/api/partners/{created.Id}", response.Headers.Location?.AbsolutePath);
    }

    // Scenario 1
    [Fact]
    public async Task Create_WithSameTaxNumberInAnotherFormat_ReturnsConflict()
    {
        await CreatePartnerAsync(" Ana Silva ", "001.234.567");

        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner("Outra Ana", "001 234 567"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal(1, await CountPartnersInDatabaseAsync());
    }

    // Scenario 1
    [Fact]
    public async Task Create_WithTaxNumberOfInactivePartner_ReturnsConflict()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "001234567");
        var body = UpdateBodyFrom(partner);
        body["isActive"] = false;
        Assert.Equal(HttpStatusCode.OK, (await PutAsync(partner.Id, body)).StatusCode);

        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner("Nova Ana", "001-234-567"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithBlankEmail_StoresNull(string email)
    {
        var created = await CreatePartnerAsync("Ana Silva", "123456789", email: email);

        Assert.Null(created.Email);
    }

    [Fact]
    public async Task Create_TrimsEmail()
    {
        var created = await CreatePartnerAsync("Ana Silva", "123456789", email: "  ana@example.com  ");

        Assert.Equal("ana@example.com", created.Email);
    }

    // Scenario 6
    [Fact]
    public async Task Create_WithTooManyDecimalPlaces_ReturnsBadRequestWithoutChanges()
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner("Ana Silva", "123456789", creditLimit: 10.999m));

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("CreditLimit", out _));
        Assert.Equal(0, await CountPartnersInDatabaseAsync());
    }

    // Scenario 6
    [Theory]
    [InlineData("12345678A")]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    [InlineData("１２３４５６７８９")]
    [InlineData("")]
    public async Task Create_WithInvalidTaxNumber_ReturnsBadRequestWithoutChanges(string taxNumber)
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner("Ana Silva", taxNumber));

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("TaxNumber", out _));
        Assert.Equal(0, await CountPartnersInDatabaseAsync());
    }

    // Scenario 6
    [Theory]
    [InlineData("CLIENT")]
    [InlineData("1")]
    public async Task Create_WithInvalidType_ReturnsBadRequestWithoutChanges(string type)
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner("Ana Silva", "123456789", type));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(0, await CountPartnersInDatabaseAsync());
    }

    [Theory]
    [InlineData("A")]
    [InlineData("   ")]
    public async Task Create_WithInvalidName_ReturnsBadRequest(string name)
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner(name, "123456789"));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1000000.01)]
    public async Task Create_WithCreditLimitOutOfRange_ReturnsBadRequest(decimal creditLimit)
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner("Ana Silva", "123456789", creditLimit: creditLimit));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithInvalidEmail_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner("Ana Silva", "123456789", email: "not-an-email"));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WhenPartnerDoesNotExist_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"{PartnersUrl}/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
