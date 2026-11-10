using MatchdayHub.Domain.Entities;
using MatchdayHub.Infrastructure.Aggregator;
using MatchdayHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>
/// Reconciles reference data (competitions, teams, fixtures) from the aggregator into
/// our database. Upserts are keyed on upstream external ids, so re-running is safe.
/// Live-owned fields advance only with a forward status transition, or with a same-status
/// score snapshot that does not reduce either side, so a stale poll cannot undo live events.
/// </summary>
public sealed class FixtureSyncService(
    MatchdayDbContext db,
    IAggregatorClient client,
    ILogger<FixtureSyncService> logger)
{
    public async Task<int> SyncAsync(CancellationToken cancellationToken = default)
    {
        var fixtures = await client.GetMatchesAsync(cancellationToken);
        if (fixtures.Count == 0)
        {
            return 0;
        }

        var competitions = await UpsertCompetitionsAsync(fixtures, cancellationToken);
        var teams = await UpsertTeamsAsync(fixtures, cancellationToken);
        var changed = await UpsertMatchesAsync(fixtures, competitions, teams, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Fixture sync complete: {Count} fixtures reconciled.", fixtures.Count);
        return changed;
    }

    private async Task<Dictionary<int, Competition>> UpsertCompetitionsAsync(
        IReadOnlyList<AggregatorMatch> fixtures, CancellationToken cancellationToken)
    {
        var byExternalId = await db.Competitions
            .Where(c => c.ExternalId != null)
            .ToDictionaryAsync(c => c.ExternalId!.Value, cancellationToken);

        foreach (var group in fixtures.GroupBy(f => f.CompetitionExternalId))
        {
            var sample = group.First();
            if (!byExternalId.TryGetValue(group.Key, out var competition))
            {
                competition = new Competition { ExternalId = group.Key };
                db.Competitions.Add(competition);
                byExternalId[group.Key] = competition;
            }

            competition.Name = sample.CompetitionName;
            competition.Country = sample.Country;
            competition.Season = sample.Season;
        }

        return byExternalId;
    }

    private async Task<Dictionary<int, Team>> UpsertTeamsAsync(
        IReadOnlyList<AggregatorMatch> fixtures, CancellationToken cancellationToken)
    {
        var byExternalId = await db.Teams
            .Where(t => t.ExternalId != null)
            .ToDictionaryAsync(t => t.ExternalId!.Value, cancellationToken);

        var refs = fixtures
            .SelectMany(f => new[] { f.Home, f.Away })
            .GroupBy(t => t.ExternalId)
            .Select(g => g.First());

        foreach (var teamRef in refs)
        {
            if (!byExternalId.TryGetValue(teamRef.ExternalId, out var team))
            {
                team = new Team { ExternalId = teamRef.ExternalId };
                db.Teams.Add(team);
                byExternalId[teamRef.ExternalId] = team;
            }

            team.Name = teamRef.Name;
            team.ShortName = teamRef.ShortName;
            team.Stadium = teamRef.Stadium;
        }

        return byExternalId;
    }

    private async Task<int> UpsertMatchesAsync(
        IReadOnlyList<AggregatorMatch> fixtures,
        Dictionary<int, Competition> competitions,
        Dictionary<int, Team> teams,
        CancellationToken cancellationToken)
    {
        var existing = await db.Matches
            .Where(m => m.ExternalId != null)
            .ToDictionaryAsync(m => m.ExternalId!.Value, cancellationToken);

        var changed = 0;
        foreach (var fixture in fixtures)
        {
            if (!competitions.TryGetValue(fixture.CompetitionExternalId, out var competition) ||
                !teams.TryGetValue(fixture.Home.ExternalId, out var home) ||
                !teams.TryGetValue(fixture.Away.ExternalId, out var away))
            {
                continue;
            }

            if (!existing.TryGetValue(fixture.ExternalId, out var match))
            {
                // First sight of this fixture: seed the live-owned fields too.
                match = new Match
                {
                    ExternalId = fixture.ExternalId,
                    Status = UpstreamMapping.ToMatchStatus(fixture.Status),
                    HomeScore = fixture.HomeScore,
                    AwayScore = fixture.AwayScore,
                };
                db.Matches.Add(match);
                existing[fixture.ExternalId] = match;
                changed++;
            }

            else
            {
                ReconcileLiveFields(match, fixture);
            }

            // Reference fields are always reconciled; live fields use a monotonic snapshot rule.
            match.Competition = competition;
            match.CompetitionId = competition.Id;
            match.HomeTeam = home;
            match.HomeTeamId = home.Id;
            match.AwayTeam = away;
            match.AwayTeamId = away.Id;
            match.KickoffUtc = fixture.KickoffUtc;
        }

        return changed;
    }

    private static void ReconcileLiveFields(Match match, AggregatorMatch fixture)
    {
        var incomingStatus = UpstreamMapping.ToMatchStatus(fixture.Status);
        if ((int)incomingStatus > (int)match.Status)
        {
            match.Status = incomingStatus;
            match.HomeScore = fixture.HomeScore;
            match.AwayScore = fixture.AwayScore;
            return;
        }

        if (incomingStatus == match.Status &&
            fixture.HomeScore >= match.HomeScore && fixture.AwayScore >= match.AwayScore)
        {
            match.HomeScore = fixture.HomeScore;
            match.AwayScore = fixture.AwayScore;
        }
    }
}
