namespace MatchdayHub.Domain.Analytics;

/// <summary>Per-player physical-load line within a match report.</summary>
public sealed record PlayerLoadLine(
    string PlayerName,
    Guid TeamId,
    double DistanceKm,
    int Sprints,
    double HighIntensityMinutes);

/// <summary>
/// Aggregated player-load report for a single match: per-player lines plus
/// squad totals/averages, ordered by distance covered.
/// </summary>
public sealed record PlayerLoadReport(
    Guid MatchId,
    int PlayerCount,
    double TotalDistanceKm,
    double AverageDistanceKm,
    int TotalSprints,
    double TotalHighIntensityMinutes,
    PlayerLoadLine? TopDistancePlayer,
    IReadOnlyList<PlayerLoadLine> Players);
