using Microsoft.Extensions.Options;

namespace MatchdayHub.Infrastructure.Aggregator;

/// <summary>
/// Deterministic, in-memory implementation of <see cref="IAggregatorClient"/>. It lets
/// the whole stack boot and stream live events without the real aggregator being up
/// (the "fixture/fake mode" from the brief) — analogous to a Node service running
/// against an MSW mock server. The live match's timeline is revealed one event at a
/// time on a fixed cadence, so each poll surfaces a little more of the game.
/// </summary>
public sealed class FakeAggregatorClient : IAggregatorClient
{
    private readonly TimeSpan _revealInterval;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    public FakeAggregatorClient(IOptions<AggregatorFakeOptions> options)
    {
        var seconds = Math.Max(1, options.Value.RevealIntervalSeconds);
        _revealInterval = TimeSpan.FromSeconds(seconds);
    }

    public Task<IReadOnlyList<AggregatorMatch>> GetMatchesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(DemoCatalog.Matches);

    public Task<IReadOnlyList<AggregatorEvent>> GetLiveEventsAsync(CancellationToken cancellationToken = default)
    {
        var elapsed = DateTimeOffset.UtcNow - _startedAt;
        // Reveal one more scripted event per interval; re-send everything revealed so
        // far each poll, which also exercises the consumer's idempotency.
        var revealed = (int)(elapsed.Ticks / _revealInterval.Ticks) + 1;
        var count = Math.Clamp(revealed, 1, DemoCatalog.LiveEventScript.Count);

        IReadOnlyList<AggregatorEvent> events = DemoCatalog.LiveEventScript.Take(count).ToList();
        return Task.FromResult(events);
    }
}

/// <summary>Options for the fake client's live-event reveal cadence.</summary>
public sealed class AggregatorFakeOptions
{
    public const string SectionName = "Aggregator:Fake";

    /// <summary>Seconds between each newly revealed live event.</summary>
    public int RevealIntervalSeconds { get; set; } = 8;
}
