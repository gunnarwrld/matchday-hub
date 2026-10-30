using MatchdayHub.Infrastructure.Aggregator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>
/// Producer side of ingestion. Reconciles fixtures once on start, then polls the
/// aggregator for live events on a fixed cadence and enqueues each one. This is the
/// role a BullMQ repeatable job plays in the Node service — here it is a
/// <see cref="BackgroundService"/>. It never writes to the database directly; all
/// persistence happens in the consumer, keeping this side cheap and stateless.
/// </summary>
public sealed class MatchPoller(
    IServiceScopeFactory scopeFactory,
    IEventQueue queue,
    IOptions<IngestionOptions> options,
    ILogger<MatchPoller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var interval = TimeSpan.FromSeconds(Math.Max(1, settings.PollIntervalSeconds));

        if (settings.SyncFixturesOnStart)
        {
            await RunFixtureSyncAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Poll iteration failed; will retry next tick.");
                await SafeDelayAsync(interval, stoppingToken);
            }
        }
    }

    private async Task RunFixtureSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<FixtureSyncService>();
            await sync.SyncAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Initial fixture sync failed.");
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IAggregatorClient>();

        var events = await client.GetLiveEventsAsync(cancellationToken);
        foreach (var e in events)
        {
            var message = new LiveEventMessage(
                MessageId: e.UpstreamEventId,
                MatchExternalId: e.MatchExternalId,
                TeamExternalId: e.TeamExternalId,
                PlayerName: e.PlayerName,
                Type: UpstreamMapping.ToEventType(e.Type),
                Minute: e.Minute,
                HomeScore: e.HomeScore,
                AwayScore: e.AwayScore,
                MatchStatus: UpstreamMapping.ToMatchStatus(e.MatchStatus));

            await queue.EnqueueAsync(message, cancellationToken);
        }
    }

    private static async Task SafeDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }
}
