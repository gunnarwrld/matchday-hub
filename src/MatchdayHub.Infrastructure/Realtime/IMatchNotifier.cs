namespace MatchdayHub.Infrastructure.Realtime;

/// <summary>
/// Abstraction over the realtime transport. Implemented by
/// <see cref="SignalRMatchNotifier"/>; keeping the ingestion consumer behind this
/// interface means the consumer has no direct SignalR dependency and stays unit-testable.
/// </summary>
public interface IMatchNotifier
{
    Task MatchEventAsync(MatchEventBroadcast broadcast, CancellationToken cancellationToken = default);

    Task ScoreUpdateAsync(ScoreUpdateBroadcast broadcast, CancellationToken cancellationToken = default);
}
