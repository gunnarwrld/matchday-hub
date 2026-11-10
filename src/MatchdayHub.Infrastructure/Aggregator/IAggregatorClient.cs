namespace MatchdayHub.Infrastructure.Aggregator;

/// <summary>
/// Read-only client for the upstream Fútbol Data Aggregator. Two implementations:
/// <see cref="HttpAggregatorClient"/> (typed <c>HttpClient</c> + resilience) and
/// <see cref="FakeAggregatorClient"/> (deterministic in-memory fixtures).
/// </summary>
public interface IAggregatorClient
{
    /// <summary>All known fixtures — reference data for the fixture-sync pass.</summary>
    Task<IReadOnlyList<AggregatorMatch>> GetMatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A flat stream of the latest timeline events across every live fixture, each
    /// carrying the match's current score snapshot. The poller enqueues these for the
    /// ingestion consumer.
    /// </summary>
    Task<IReadOnlyList<AggregatorEvent>> GetLiveEventsAsync(CancellationToken cancellationToken = default);
}
