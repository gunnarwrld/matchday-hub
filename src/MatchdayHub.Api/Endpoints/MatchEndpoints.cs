using MatchdayHub.Domain.Enums;
using MatchdayHub.Infrastructure.Reads;

namespace MatchdayHub.Api.Endpoints;

/// <summary>
/// Minimal-API route handlers for the v1 surface. Minimal APIs are the .NET analogue of
/// Express route handlers; <c>TypedResults</c> gives a compile-checked status code and a
/// documented response shape (what <c>res.status(...).json(...)</c> does, but typed).
/// </summary>
public static class MatchEndpoints
{
    public const string TeamFormCachePolicy = "team-form";

    public static RouteGroupBuilder MapMatchEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/matches", GetMatches)
            .WithName("ListMatches")
            .WithSummary("List matches with paging and filters.");

        group.MapGet("/matches/{id:guid}", GetMatchById)
            .WithName("GetMatch")
            .WithSummary("Get a single match with its event timeline.");

        group.MapGet("/matches/{id:guid}/events", GetMatchEvents)
            .WithName("GetMatchEvents")
            .WithSummary("Get the event timeline for a match.");

        group.MapGet("/matches/{id:guid}/players/load", GetPlayerLoad)
            .WithName("GetMatchPlayerLoad")
            .WithSummary("Get aggregated player physical-load for a match.");

        group.MapGet("/teams/{id:guid}/form", GetTeamForm)
            .WithName("GetTeamForm")
            .WithSummary("Get a team's recent form (Redis output-cached).")
            .CacheOutput(TeamFormCachePolicy);

        return group;
    }

    private static async Task<IResult> GetMatches(
        MatchReadService reads,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        Guid? competitionId = null,
        Guid? teamId = null,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors[nameof(page)] = ["page must be 1 or greater."];
        }

        if (pageSize is < 1 or > MatchReadService.MaxPageSize)
        {
            errors[nameof(pageSize)] = [$"pageSize must be between 1 and {MatchReadService.MaxPageSize}."];
        }

        MatchStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<MatchStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
            }
            else
            {
                errors[nameof(status)] = [$"status must be one of: {string.Join(", ", Enum.GetNames<MatchStatus>())}."];
            }
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var query = new MatchQuery(page, pageSize, parsedStatus, competitionId, teamId, fromUtc, toUtc);
        var result = await reads.GetMatchesAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetMatchById(
        Guid id, MatchReadService reads, CancellationToken cancellationToken)
    {
        var match = await reads.GetMatchByIdAsync(id, cancellationToken);
        return match is null ? NotFound(id, "match") : TypedResults.Ok(match);
    }

    private static async Task<IResult> GetMatchEvents(
        Guid id, MatchReadService reads, CancellationToken cancellationToken)
    {
        var events = await reads.GetMatchEventsAsync(id, cancellationToken);
        return events is null ? NotFound(id, "match") : TypedResults.Ok(events);
    }

    private static async Task<IResult> GetPlayerLoad(
        Guid id, MatchReadService reads, CancellationToken cancellationToken)
    {
        var report = await reads.GetMatchPlayerLoadAsync(id, cancellationToken);
        return report is null ? NotFound(id, "match") : TypedResults.Ok(report);
    }

    private static async Task<IResult> GetTeamForm(
        Guid id, MatchReadService reads, CancellationToken cancellationToken, int window = 5)
    {
        if (window is < 1 or > 20)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(window)] = ["window must be between 1 and 20."],
            });
        }

        var form = await reads.GetTeamFormAsync(id, window, cancellationToken);
        return form is null ? NotFound(id, "team") : TypedResults.Ok(form);
    }

    private static IResult NotFound(Guid id, string resource) =>
        TypedResults.Problem(
            title: "Resource not found",
            detail: $"No {resource} exists with id '{id}'.",
            statusCode: StatusCodes.Status404NotFound);
}
