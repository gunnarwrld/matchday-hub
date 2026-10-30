namespace MatchdayHub.Domain.Entities;

/// <summary>
/// A league or cup. Maps to the aggregator's <c>League</c> model.
/// </summary>
public class Competition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable id from the upstream aggregator, used for idempotent upserts.</summary>
    public int? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Season { get; set; } = string.Empty;

    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
