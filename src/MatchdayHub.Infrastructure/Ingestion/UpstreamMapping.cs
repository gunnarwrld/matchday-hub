using MatchdayHub.Domain.Enums;

namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>
/// Normalises the aggregator's free-form status/event strings into our closed enums.
/// Centralised so the poller, seeder, and tests all agree on the mapping.
/// </summary>
public static class UpstreamMapping
{
    public static MatchStatus ToMatchStatus(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "SCHEDULED" or "NS" or "TBD" or "POSTPONED" => MatchStatus.Scheduled,
        "FINISHED" or "FT" or "AET" or "PEN" or "MATCH FINISHED" => MatchStatus.Finished,
        _ => MatchStatus.Live,
    };

    public static MatchEventType ToEventType(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "GOAL" => MatchEventType.Goal,
        "YELLOW_CARD" or "YELLOW CARD" => MatchEventType.YellowCard,
        "RED_CARD" or "RED CARD" => MatchEventType.RedCard,
        "SUBSTITUTION" or "SUBST" => MatchEventType.Substitution,
        "VAR" => MatchEventType.Var,
        _ => MatchEventType.Unknown,
    };
}
