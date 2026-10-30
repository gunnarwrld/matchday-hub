namespace MatchdayHub.Infrastructure.Aggregator;

/// <summary>Normalised team reference returned by <see cref="IAggregatorClient"/>.</summary>
public sealed record AggregatorTeamRef(
    int ExternalId,
    string Name,
    string ShortName,
    string Stadium);

/// <summary>Normalised fixture returned by <see cref="IAggregatorClient"/>.</summary>
public sealed record AggregatorMatch(
    int ExternalId,
    int CompetitionExternalId,
    string CompetitionName,
    string Country,
    string Season,
    AggregatorTeamRef Home,
    AggregatorTeamRef Away,
    DateTimeOffset KickoffUtc,
    string Status,
    int HomeScore,
    int AwayScore);

/// <summary>Normalised timeline event returned by <see cref="IAggregatorClient"/>.</summary>
public sealed record AggregatorEvent(
    string UpstreamEventId,
    int MatchExternalId,
    int TeamExternalId,
    string? PlayerName,
    string Type,
    int Minute,
    int HomeScore,
    int AwayScore,
    string MatchStatus);
