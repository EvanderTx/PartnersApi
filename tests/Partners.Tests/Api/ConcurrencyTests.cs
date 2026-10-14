using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Partners.Domain.Partners;
using Partners.Tests.Infrastructure;

namespace Partners.Tests.Api;

// Scenario 5: every request runs in its own DI scope, so each one has an independent DbContext and SQL connection.
public sealed class ConcurrencyTests(PartnersApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task SimultaneousCreates_WithSameTaxNumber_CreateExactlyOnePartner()
    {
        const int requestCount = 10;
        var requests = Enumerable.Range(0, requestCount).Select(i =>
            Factory.CreateClient().PostAsJsonAsync(
                PartnersUrl,
                NewPartner($"Pedido {i}", i % 2 == 0 ? "987.654.321" : "987 654 321")));

        var responses = await Task.WhenAll(requests);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(requestCount - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        await using var dbContext = Factory.CreateDbContext();
        Assert.Equal(1, await dbContext.Partners.CountAsync(p => p.TaxNumber == "987654321"));
    }

    [Fact]
    public async Task SimultaneousUpdates_WithSameVersion_OnlyOneSucceeds()
    {
        var partner = await CreatePartnerAsync("Ana Silva", "123456789");
        var requests = Enumerable.Range(0, 2).Select(i =>
        {
            var body = UpdateBodyFrom(partner);
            body["name"] = $"Alteracao {i}";
            return Factory.CreateClient().PutAsJsonAsync($"{PartnersUrl}/{partner.Id}", body);
        });

        var responses = await Task.WhenAll(requests);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var winnerIndex = Array.FindIndex(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal($"Alteracao {winnerIndex}", (await GetAsync(partner.Id)).Name);
    }

    [Fact]
    public async Task IndependentContexts_UpdatingSameVersion_SecondSaveFails()
    {
        var created = await CreatePartnerAsync("Ana Silva", "123456789");
        await using var first = Factory.CreateDbContext();
        await using var second = Factory.CreateDbContext();
        var firstCopy = await first.Partners.SingleAsync(p => p.Id == created.Id);
        var secondCopy = await second.Partners.SingleAsync(p => p.Id == created.Id);

        firstCopy.Update("Primeiro", "123456789", PartnerType.Customer, null, 100m, true, DateTime.UtcNow);
        secondCopy.Update("Segundo", "123456789", PartnerType.Customer, null, 100m, true, DateTime.UtcNow);
        await first.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        Assert.Equal("Primeiro", (await GetAsync(created.Id)).Name);
    }

    [Fact]
    public async Task IndependentContexts_InsertingSameTaxNumber_DatabaseRejectsOne()
    {
        await using var first = Factory.CreateDbContext();
        await using var second = Factory.CreateDbContext();
        first.Partners.Add(Partner.Create("Primeiro", "555555555", PartnerType.Customer, null, 0m, DateTime.UtcNow));
        second.Partners.Add(Partner.Create("Segundo", "555-555-555", PartnerType.Supplier, null, 0m, DateTime.UtcNow));

        var results = await Task.WhenAll(TrySaveAsync(first), TrySaveAsync(second));

        Assert.Single(results, succeeded => succeeded);
        Assert.Equal(1, await CountPartnersInDatabaseAsync());
    }

    private static async Task<bool> TrySaveAsync(DbContext dbContext)
    {
        try
        {
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
