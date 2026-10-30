using MatchdayHub.Domain.Enums;

namespace MatchdayHub.Domain.Entities;

/// <summary>A single fixture between two teams. Maps to the aggregator's <c>Match</c> model.</summary>
public class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable id from the upstream aggregator, used for idempotent upserts.</summary>
    public int? ExternalId { get; set; }

    public Guid CompetitionId { get; set; }

    public Guid HomeTeamId { get; set; }

    public Guid AwayTeamId { get; set; }

    /// <summary>Kickoff time, always stored in UTC.</summary>
    public DateTimeOffset KickoffUtc { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    public Competition? Competition { get; set; }

    public Team? HomeTeam { get; set; }

    public Team? AwayTeam { get; set; }

    public ICollection<MatchEvent> Events { get; set; } = new List<MatchEvent>();

    public ICollection<PlayerMatchLoad> PlayerLoads { get; set; } = new List<PlayerMatchLoad>();
}
