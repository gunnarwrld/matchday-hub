namespace MatchdayHub.Domain.Entities;

/// <summary>A club. Maps to the aggregator's <c>Team</c> model.</summary>
public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable id from the upstream aggregator, used for idempotent upserts.</summary>
    public int? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public string Stadium { get; set; } = string.Empty;

    public ICollection<Match> HomeMatches { get; set; } = new List<Match>();

    public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}
