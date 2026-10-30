using MatchdayHub.Domain.Entities;

namespace MatchdayHub.Domain.Analytics;

/// <summary>
/// Pure aggregation of per-player load rows into a match-level report.
/// Lives in Domain so the maths is unit-tested without touching the database.
/// </summary>
public static class PlayerLoadAggregator
{
    /// <summary>Decimal places used for the distance/intensity aggregates.</summary>
    public const int Precision = 2;

    public static PlayerLoadReport Summarise(Guid matchId, IEnumerable<PlayerMatchLoad> loads)
    {
        ArgumentNullException.ThrowIfNull(loads);

        var players = loads
            .Select(l => new PlayerLoadLine(
                l.PlayerName,
                l.TeamId,
                Math.Round(l.DistanceKm, Precision),
                l.Sprints,
                Math.Round(l.HighIntensityMinutes, Precision)))
            .OrderByDescending(l => l.DistanceKm)
            .ThenBy(l => l.PlayerName, StringComparer.Ordinal)
            .ToList();

        if (players.Count == 0)
        {
            return new PlayerLoadReport(matchId, 0, 0, 0, 0, 0, null, players);
        }

        var totalDistance = players.Sum(p => p.DistanceKm);
        var totalHigh = players.Sum(p => p.HighIntensityMinutes);

        return new PlayerLoadReport(
            MatchId: matchId,
            PlayerCount: players.Count,
            TotalDistanceKm: Math.Round(totalDistance, Precision),
            AverageDistanceKm: Math.Round(totalDistance / players.Count, Precision),
            TotalSprints: players.Sum(p => p.Sprints),
            TotalHighIntensityMinutes: Math.Round(totalHigh, Precision),
            TopDistancePlayer: players[0],
            Players: players);
    }
}
