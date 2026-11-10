namespace MatchdayHub.Infrastructure.Realtime;

/// <summary>Payload pushed to clients when a new match event is ingested.</summary>
public sealed record MatchEventBroadcast(
    Guid MatchId,
    string Type,
    int Minute,
    string? PlayerName,
    Guid TeamId,
    int HomeScore,
    int AwayScore);

/// <summary>Payload pushed to clients when a match's score or status changes.</summary>
public sealed record ScoreUpdateBroadcast(
    Guid MatchId,
    int HomeScore,
    int AwayScore,
    string Status);

/// <summary>Client-callable method names, kept in one place to avoid magic strings.</summary>
public static class MatchHubMethods
{
    public const string MatchEvent = "matchEvent";
    public const string ScoreUpdate = "scoreUpdate";
}
