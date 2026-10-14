using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Partners.Application.Common;
using Partners.Application.Partners.Dtos;

namespace Partners.Tests.Infrastructure;

[Collection(ApiTestCollection.Name)]
public abstract class ApiTestBase(PartnersApiFactory factory) : IAsyncLifetime
{
    protected const string PartnersUrl = "/api/partners";

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    protected PartnersApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    // Every test starts with an empty Partners table.
    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected static object NewPartner(
        string name, string taxNumber, string type = "CUSTOMER", decimal creditLimit = 100m, string? email = null) =>
        new { name, taxNumber, type, email, creditLimit };

    protected async Task<PartnerResponse> CreatePartnerAsync(
        string name, string taxNumber, string type = "CUSTOMER", decimal creditLimit = 100m, string? email = null)
    {
        var response = await Client.PostAsJsonAsync(PartnersUrl, NewPartner(name, taxNumber, type, creditLimit, email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<PartnerResponse>(response);
    }

    // Builds a complete PUT body from the current state; tests override only what they change.
    protected static Dictionary<string, object?> UpdateBodyFrom(PartnerResponse partner) => new()
    {
        ["name"] = partner.Name,
        ["taxNumber"] = partner.TaxNumber,
        ["type"] = partner.Type.ToString().ToUpperInvariant(),
        ["email"] = partner.Email,
        ["creditLimit"] = partner.CreditLimit,
        ["isActive"] = partner.IsActive,
        ["version"] = partner.Version
    };

    protected Task<HttpResponseMessage> PutAsync(Guid id, object body) =>
        Client.PutAsJsonAsync($"{PartnersUrl}/{id}", body);

    protected async Task<PartnerResponse> GetAsync(Guid id)
    {
        var response = await Client.GetAsync($"{PartnersUrl}/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync<PartnerResponse>(response);
    }

    protected async Task<PagedResult<PartnerResponse>> ListAsync(string queryString = "")
    {
        var response = await Client.GetAsync($"{PartnersUrl}{queryString}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync<PagedResult<PartnerResponse>>(response);
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;

    // Errors must be ProblemDetails with the expected status and never expose stack traces.
    protected static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("   at ", body);

        var problem = JsonDocument.Parse(body).RootElement;
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        return problem;
    }

    protected async Task<int> CountPartnersInDatabaseAsync()
    {
        await using var dbContext = Factory.CreateDbContext();
        return await dbContext.Partners.CountAsync();
    }
}
