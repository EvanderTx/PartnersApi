using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Partners.Infrastructure.Persistence;

namespace Partners.Tests.Infrastructure;

// Starts the real API in memory against a dedicated SQL Server database (never the development one).
public sealed class PartnersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=PartnersDb_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("PARTNERS_TEST_CONNECTION_STRING") ?? DefaultConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PartnersDb", ConnectionString);
    }

    // Each call returns an independent context with its own SQL Server connection.
    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Partners.ExecuteDeleteAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
