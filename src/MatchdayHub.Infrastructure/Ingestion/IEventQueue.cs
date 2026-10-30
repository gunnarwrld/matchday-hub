namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>
/// Transport abstraction between the poller (producer) and the ingestion consumer.
/// The local implementation is an in-process <c>System.Threading.Channels</c> channel
/// (<see cref="ChannelEventQueue"/>); swapping in Azure Service Bus later is just a new
/// implementation of this interface — nothing else changes. This is the BullMQ queue
/// boundary, expressed as a DI seam.
/// </summary>
public interface IEventQueue
{
    /// <summary>Enqueue a message for the consumer.</summary>
    ValueTask EnqueueAsync(LiveEventMessage message, CancellationToken cancellationToken = default);

    /// <summary>Stream messages as they arrive. Completes when the host shuts down.</summary>
    IAsyncEnumerable<LiveEventMessage> ReadAllAsync(CancellationToken cancellationToken = default);
}
