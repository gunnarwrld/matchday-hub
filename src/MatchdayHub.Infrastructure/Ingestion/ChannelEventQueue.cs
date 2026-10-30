using System.Threading.Channels;

namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>
/// In-process queue backed by a bounded <see cref="Channel{T}"/>. Registered as a
/// singleton so the poller and consumer share one channel. The bound applies
/// backpressure (<see cref="BoundedChannelFullMode.Wait"/>), the same role Redis/BullMQ
/// plays across processes — here it is a single-process stand-in for local dev.
/// </summary>
public sealed class ChannelEventQueue : IEventQueue
{
    private readonly Channel<LiveEventMessage> _channel =
        Channel.CreateBounded<LiveEventMessage>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

    public ValueTask EnqueueAsync(LiveEventMessage message, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<LiveEventMessage> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
