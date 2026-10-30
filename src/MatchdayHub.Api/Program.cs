using System.Threading.RateLimiting;
using Asp.Versioning;
using MatchdayHub.Api.Endpoints;
using MatchdayHub.Infrastructure;
using MatchdayHub.Infrastructure.Demo;
using MatchdayHub.Infrastructure.Ingestion;
using MatchdayHub.Infrastructure.Persistence;
using MatchdayHub.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var redisConnection = builder.Configuration.GetConnectionString(DependencyInjection.RedisConnectionName);
var postgresConnection = builder.Configuration.GetConnectionString(DependencyInjection.PostgresConnectionName);

// ── Infrastructure: persistence, aggregator client, queue, notifier ──────────────
builder.Services.AddMatchdayInfrastructure(builder.Configuration);
builder.Services.AddMatchdayRealtime(builder.Configuration);

// For the single-process local demo the API also hosts the ingestion pipeline so
// `dotnet run` streams live events with no separate worker. Disable to run the Worker.
if (builder.Configuration.GetValue("Ingestion:RunInApi", true))
{
    builder.Services.AddMatchdayIngestionWorkers();
}

// ── API cross-cutting concerns ───────────────────────────────────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddOutputCache(options =>
    options.AddPolicy(MatchEndpoints.TeamFormCachePolicy, policy => policy.Expire(TimeSpan.FromSeconds(30))));
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisOutputCache(options => options.Configuration = redisConnection);
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(postgresConnection))
{
    healthChecks.AddNpgSql(postgresConnection, name: "postgres", tags: ["ready"]);
}

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    healthChecks.AddRedis(redisConnection, name: "redis", tags: ["ready"]);
}

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseOutputCache();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Matchday Hub API"));

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var v1 = app.MapGroup("/v{version:apiVersion}")
    .WithApiVersionSet(versionSet)
    .HasApiVersion(new ApiVersion(1, 0));
v1.MapMatchEndpoints();

app.MapHub<MatchHub>("/hubs/matches");

// Liveness: no dependency probes, so a transient Postgres/Redis blip takes the instance
// out of rotation (readiness) without triggering a process restart (liveness).
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});

app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

await InitializeDatabaseAsync(app);

await app.RunAsync();

// ── Startup database init: migrate, reconcile fixtures, seed demo data ──────────
static async Task InitializeDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var services = scope.ServiceProvider;

    if (app.Configuration.GetValue("Database:AutoMigrate", true))
    {
        var db = services.GetRequiredService<MatchdayDbContext>();
        await db.Database.MigrateAsync();
    }

    if (app.Configuration.GetValue("SeedOnStartup", true))
    {
        await services.GetRequiredService<FixtureSyncService>().SyncAsync();
        await services.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }
}

/// <summary>Exposed so the integration-test <c>WebApplicationFactory</c> can boot the app.</summary>
public partial class Program;
