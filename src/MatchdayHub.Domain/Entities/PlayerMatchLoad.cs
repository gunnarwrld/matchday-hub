namespace MatchdayHub.Domain.Entities;

/// <summary>
/// Physical-load metrics for one player in one match. The upstream aggregator has no
/// player-stats model, so Matchday Hub owns this data (seeded demo data today; a
/// tracking-vendor feed later). This is the "team-sport data" angle for the role.
/// </summary>
public class PlayerMatchLoad
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }

    public Guid TeamId { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Total distance covered, in kilometres.</summary>
    public double DistanceKm { get; set; }

    /// <summary>Number of sprints (efforts above the sprint-speed threshold).</summary>
    public int Sprints { get; set; }

    /// <summary>Minutes spent in high-intensity zones.</summary>
    public double HighIntensityMinutes { get; set; }

    public Match? Match { get; set; }

    public Team? Team { get; set; }
}
