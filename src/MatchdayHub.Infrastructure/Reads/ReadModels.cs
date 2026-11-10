using MatchdayHub.Domain.Enums;

namespace MatchdayHub.Infrastructure.Reads;

/// <summary>A generic paged envelope for list endpoints.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

/// <summary>Filter + paging inputs for the matches list.</summary>
public sealed record MatchQuery(
    int Page,
    int PageSize,
    MatchStatus? Status,
    Guid? CompetitionId,
    Guid? TeamId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);

public sealed record TeamRefDto(Guid Id, string Name, string ShortName);

public sealed record MatchSummaryDto(
    Guid Id,
    int? ExternalId,
    string Competition,
    TeamRefDto Home,
    TeamRefDto Away,
    DateTimeOffset KickoffUtc,
    string Status,
    int HomeScore,
    int AwayScore);

public sealed record MatchEventDto(
    Guid Id,
    string UpstreamEventId,
    string Type,
    int Minute,
    string? PlayerName,
    Guid TeamId,
    DateTimeOffset CreatedAt);

public sealed record MatchDetailDto(
    Guid Id,
    int? ExternalId,
    string Competition,
    string Country,
    string Season,
    TeamRefDto Home,
    TeamRefDto Away,
    DateTimeOffset KickoffUtc,
    string Status,
    int HomeScore,
    int AwayScore,
    IReadOnlyList<MatchEventDto> Events);

public sealed record TeamFormDto(
    Guid TeamId,
    string TeamName,
    int Window,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    string Form);
