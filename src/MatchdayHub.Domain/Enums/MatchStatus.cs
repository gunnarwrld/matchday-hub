namespace MatchdayHub.Domain.Enums;

/// <summary>
/// Lifecycle of a match. The upstream aggregator uses free-form strings
/// ("SCHEDULED", "LIVE", "IN_PLAY", "PAUSED", "FINISHED"); we normalise those
/// into this closed enum at the ingestion boundary.
/// </summary>
public enum MatchStatus
{
    Scheduled = 0,
    Live = 1,
    Finished = 2,
}
