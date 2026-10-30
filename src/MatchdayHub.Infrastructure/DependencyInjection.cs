using MatchdayHub.Infrastructure.Aggregator;
using MatchdayHub.Infrastructure.Demo;
using MatchdayHub.Infrastructure.Ingestion;
using MatchdayHub.Infrastructure.Persistence;
using MatchdayHub.Infrastructure.Reads;
using MatchdayHub.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatchdayHub.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer — the one place the Api and Worker
/// call to register persistence, the aggregator client, the ingestion pipeline, and
/// the realtime notifier. Mirrors a Node app's <c>container.register(...)</c> module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Connection-string name for the primary Postgres database.</summary>
    public const string PostgresConnectionName = "Postgres";

    /// <summary>Connection-string name for the Redis instance (cache + backplane).</summary>
    public const string RedisConnectionName = "Redis";

    public static IServiceCollection AddMatchdayInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PostgresConnectionName)
            ?? "Host=localhost;Port=5432;Database=matchday;Username=matchday;Password=matchday";

        services.AddDbContext<MatchdayDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(MatchdayDbContext).Assembly.FullName)));

        services.AddOptions<AggregatorOptions>()
            .Bind(configuration.GetSection(AggregatorOptions.SectionName));
        services.AddOptions<AggregatorFakeOptions>()
            .Bind(configuration.GetSection(AggregatorFakeOptions.SectionName));
        services.AddOptions<IngestionOptions>()
            .Bind(configuration.GetSection(IngestionOptions.SectionName));

        RegisterAggregatorClient(services, configuration);

        services.AddSingleton<IEventQueue, ChannelEventQueue>();
        services.AddSingleton<IMatchNotifier, SignalRMatchNotifier>();

        services.AddScoped<FixtureSyncService>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<IngestionProcessor>();
        services.AddScoped<MatchReadService>();

        return services;
    }

    /// <summary>
    /// Registers the poller + consumer background services. Hosted by the Worker and,
    /// for the single-process local demo, optionally by the Api.
    /// </summary>
    public static IServiceCollection AddMatchdayIngestionWorkers(this IServiceCollection services)
    {
        services.AddHostedService<MatchPoller>();
        services.AddHostedService<IngestionConsumer>();
        return services;
    }

    /// <summary>
    /// Adds SignalR and, when a <c>Redis</c> connection string is present, the Redis
    /// backplane so the Api and Worker share one set of client groups.
    /// </summary>
    public static ISignalRServerBuilder AddMatchdayRealtime(
        this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddSignalR();
        var redis = configuration.GetConnectionString(RedisConnectionName);
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.AddStackExchangeRedis(redis);
        }

        return builder;
    }

    private static void RegisterAggregatorClient(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AggregatorOptions.SectionName).Get<AggregatorOptions>()
            ?? new AggregatorOptions();

        if (options.UseFake)
        {
            services.AddSingleton<IAggregatorClient, FakeAggregatorClient>();
            return;
        }

        services.AddHttpClient<IAggregatorClient, HttpAggregatorClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
                }
            })
            .AddStandardResilienceHandler();
    }
}
