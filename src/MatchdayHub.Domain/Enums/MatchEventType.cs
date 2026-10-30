namespace MatchdayHub.Domain.Enums;

/// <summary>
/// Normalised match-event categories. Mirrors the aggregator's
/// <c>eventType</c> values plus an <see cref="Unknown"/> fallback so an
/// unexpected upstream value never aborts ingestion.
/// </summary>
public enum MatchEventType
{
    Unknown = 0,
    Goal = 1,
    YellowCard = 2,
    RedCard = 3,
    Substitution = 4,
    Var = 5,
}
