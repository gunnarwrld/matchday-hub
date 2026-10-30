namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>Binds the <c>Ingestion</c> configuration section.</summary>
public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    /// <summary>How often the poller asks the aggregator for new live events.</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>Run a fixture reconciliation pass when the poller starts.</summary>
    public bool SyncFixturesOnStart { get; set; } = true;
}
