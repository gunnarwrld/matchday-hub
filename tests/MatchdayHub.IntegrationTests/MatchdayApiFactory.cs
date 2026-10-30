using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace MatchdayHub.IntegrationTests;

/// <summary>
/// Boots the real API in-process against a disposable PostgreSQL container
/// (Testcontainers) — the integration-test equivalent of spinning up the app with a
/// throwaway database. The app runs in fake-aggregator mode with the background pollers
/// disabled, so tests exercise the HTTP surface and seeded data deterministically.
/// Requires Docker, which is available on GitHub Actions' ubuntu-latest runners.
/// </summary>
public sealed class MatchdayApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("matchday")
        .WithUsername("matchday")
        .WithPassword("matchday")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", string.Empty); // force in-memory cache, skip Redis health
        builder.UseSetting("Aggregator:UseFake", "true");
        builder.UseSetting("Ingestion:RunInApi", "false"); // no background pollers during tests
        builder.UseSetting("SeedOnStartup", "true"); // seed demo data so read endpoints return rows
    }
}
