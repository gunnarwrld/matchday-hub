using MatchdayHub.Domain.Enums;

namespace MatchdayHub.Domain.Entities;

/// <summary>
/// A timeline event (goal, card, substitution…) within a match.
/// Maps to the aggregator's <c>MatchEvent</c> model.
/// </summary>
public class MatchEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The upstream event id (the aggregator's <c>MatchEvent.id</c>). This is the
    /// idempotency key for ingestion: a unique index on it makes re-delivery a no-op.
    /// </summary>
    public string UpstreamEventId { get; set; } = string.Empty;

    public Guid MatchId { get; set; }

    public Guid TeamId { get; set; }

    public string? PlayerName { get; set; }

    public MatchEventType Type { get; set; } = MatchEventType.Unknown;

    public int Minute { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Match? Match { get; set; }

    public Team? Team { get; set; }
}
