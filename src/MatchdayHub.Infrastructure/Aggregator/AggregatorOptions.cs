namespace MatchdayHub.Infrastructure.Aggregator;

/// <summary>
/// Binds the <c>Aggregator</c> configuration section. When <see cref="UseFake"/> is
/// true the service runs against an in-memory fixture generator, so the whole stack
/// boots and streams live events without the real aggregator being reachable.
/// </summary>
public sealed class AggregatorOptions
{
    public const string SectionName = "Aggregator";

    /// <summary>Base URL of the Fútbol Data Aggregator (e.g. its Railway deployment).</summary>
    public string BaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>Optional API key sent as the <c>x-api-key</c> header.</summary>
    public string? ApiKey { get; set; }

    /// <summary>When true, use the deterministic in-memory fake instead of HTTP.</summary>
    public bool UseFake { get; set; } = true;
}
