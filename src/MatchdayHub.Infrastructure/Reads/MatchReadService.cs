using MatchdayHub.Domain.Analytics;
using MatchdayHub.Domain.Enums;
using MatchdayHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchdayHub.Infrastructure.Reads;

/// <summary>
/// Read-only query service behind the Web API. All queries are <c>AsNoTracking</c>
/// (no change tracking overhead), the EF Core equivalent of Prisma's plain
/// <c>findMany</c>/<c>findUnique</c> reads. A <c>null</c> return means "not found", which
/// the endpoints translate into a 404 problem-details response.
/// </summary>
public sealed class MatchReadService(MatchdayDbContext db)
{
    public const int MaxPageSize = 100;

    public async Task<PagedResult<MatchSummaryDto>> GetMatchesAsync(
        MatchQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var matches = db.Matches.AsNoTracking();

        if (query.Status is { } status)
        {
            matches = matches.Where(m => m.Status == status);
        }

        if (query.CompetitionId is { } competitionId)
        {
            matches = matches.Where(m => m.CompetitionId == competitionId);
        }

        if (query.TeamId is { } teamId)
        {
            matches = matches.Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId);
        }

        if (query.FromUtc is { } from)
        {
            matches = matches.Where(m => m.KickoffUtc >= from);
        }

        if (query.ToUtc is { } to)
        {
            matches = matches.Where(m => m.KickoffUtc <= to);
        }

        var total = await matches.CountAsync(cancellationToken);

        var items = await matches
            .OrderByDescending(m => m.KickoffUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MatchSummaryDto(
                m.Id,
                m.ExternalId,
                m.Competition!.Name,
                new TeamRefDto(m.HomeTeam!.Id, m.HomeTeam.Name, m.HomeTeam.ShortName),
                new TeamRefDto(m.AwayTeam!.Id, m.AwayTeam.Name, m.AwayTeam.ShortName),
                m.KickoffUtc,
                m.Status.ToString(),
                m.HomeScore,
                m.AwayScore))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<MatchSummaryDto>(items, page, pageSize, total, totalPages);
    }

    public async Task<MatchDetailDto?> GetMatchByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Matches
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MatchDetailDto(
                m.Id,
                m.ExternalId,
                m.Competition!.Name,
                m.Competition.Country,
                m.Competition.Season,
                new TeamRefDto(m.HomeTeam!.Id, m.HomeTeam.Name, m.HomeTeam.ShortName),
                new TeamRefDto(m.AwayTeam!.Id, m.AwayTeam.Name, m.AwayTeam.ShortName),
                m.KickoffUtc,
                m.Status.ToString(),
                m.HomeScore,
                m.AwayScore,
                m.Events
                    .OrderBy(e => e.Minute)
                    .Select(e => new MatchEventDto(
                        e.Id, e.UpstreamEventId, e.Type.ToString(), e.Minute, e.PlayerName, e.TeamId, e.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MatchEventDto>?> GetMatchEventsAsync(
        Guid matchId, CancellationToken cancellationToken = default)
    {
        var exists = await db.Matches.AnyAsync(m => m.Id == matchId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await db.MatchEvents
            .AsNoTracking()
            .Where(e => e.MatchId == matchId)
            .OrderBy(e => e.Minute)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new MatchEventDto(
                e.Id, e.UpstreamEventId, e.Type.ToString(), e.Minute, e.PlayerName, e.TeamId, e.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TeamFormDto?> GetTeamFormAsync(
        Guid teamId, int window, CancellationToken cancellationToken = default)
    {
        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished &&
                        (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
            .ToListAsync(cancellationToken);

        var form = TeamFormCalculator.Calculate(teamId, matches, window);
        return new TeamFormDto(
            form.TeamId, team.Name, window, form.Played, form.Wins, form.Draws, form.Losses,
            form.GoalsFor, form.GoalsAgainst, form.GoalDifference, form.Points, form.Form);
    }

    public async Task<PlayerLoadReport?> GetMatchPlayerLoadAsync(
        Guid matchId, CancellationToken cancellationToken = default)
    {
        var exists = await db.Matches.AnyAsync(m => m.Id == matchId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var loads = await db.PlayerMatchLoads
            .AsNoTracking()
            .Where(p => p.MatchId == matchId)
            .ToListAsync(cancellationToken);

        return PlayerLoadAggregator.Summarise(matchId, loads);
    }
}
