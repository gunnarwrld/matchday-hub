using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>
/// Consumer side of ingestion. Reads messages off <see cref="IEventQueue"/> and hands
/// each to a scoped <see cref="IngestionProcessor"/> for an idempotent upsert. A failing
/// message is logged and skipped so one bad event never stalls the stream — the queue
/// keeps flowing, the BullMQ-worker equivalent.
/// </summary>
public sealed class IngestionConsumer(
    IEventQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in queue.ReadAllAsync(stoppingToken))
            {
                await HandleAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task HandleAsync(LiveEventMessage message, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IngestionProcessor>();
            var result = await processor.ProcessAsync(message, cancellationToken);
            logger.LogDebug("Ingested {MessageId}: {Result}", message.MessageId, result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ingest message {MessageId}.", message.MessageId);
        }
    }
}
