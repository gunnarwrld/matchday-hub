using MatchdayHub.Domain.Entities;
using MatchdayHub.Infrastructure.Aggregator;
using MatchdayHub.Infrastructure.Ingestion;
using MatchdayHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MatchdayHub.Infrastructure.Demo;

/// <summary>
/// Seeds demo timelines and player-load rows for the finished matches so the
/// read endpoints return something real on a fresh database. Idempotent: existing
/// rows are detected by their natural keys and left untouched. Assumes fixtures/teams
/// already exist (run after <see cref="FixtureSyncService"/>).
/// </summary>
public sealed class DemoDataSeeder(MatchdayDbContext db, ILogger<DemoDataSeeder> logger)
{
    private const int PlayersPerTeam = 11;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var matchesByExt = await db.Matches
            .Where(m => m.ExternalId != null)
            .ToDictionaryAsync(m => m.ExternalId!.Value, cancellationToken);
        var teamsByExt = await db.Teams
            .Where(t => t.ExternalId != null)
            .ToDictionaryAsync(t => t.ExternalId!.Value, cancellationToken);

        if (matchesByExt.Count == 0 || teamsByExt.Count == 0)
        {
            logger.LogWarning("Skipping demo seed: fixtures/teams not present yet.");
            return;
        }

        await SeedFinishedEventsAsync(matchesByExt, teamsByExt, cancellationToken);
        await SeedPlayerLoadsAsync(matchesByExt, teamsByExt, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFinishedEventsAsync(
        IReadOnlyDictionary<int, Match> matchesByExt,
        IReadOnlyDictionary<int, Team> teamsByExt,
        CancellationToken cancellationToken)
    {
        var existingIds = await db.MatchEvents
            .Select(e => e.UpstreamEventId)
            .ToListAsync(cancellationToken);
        var existing = existingIds.ToHashSet(StringComparer.Ordinal);

        foreach (var ev in DemoCatalog.FinishedEvents)
        {
            if (existing.Contains(ev.UpstreamEventId) ||
                !matchesByExt.TryGetValue(ev.MatchExternalId, out var match) ||
                !teamsByExt.TryGetValue(ev.TeamExternalId, out var team))
            {
                continue;
            }

            db.MatchEvents.Add(new MatchEvent
            {
                UpstreamEventId = ev.UpstreamEventId,
                MatchId = match.Id,
                TeamId = team.Id,
                PlayerName = ev.PlayerName,
                Type = UpstreamMapping.ToEventType(ev.Type),
                Minute = ev.Minute,
            });
            existing.Add(ev.UpstreamEventId);
        }
    }

    private async Task SeedPlayerLoadsAsync(
        IReadOnlyDictionary<int, Match> matchesByExt,
        IReadOnlyDictionary<int, Team> teamsByExt,
        CancellationToken cancellationToken)
    {
        var matchesWithLoads = (await db.PlayerMatchLoads
                .Select(p => p.MatchId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var fixture in DemoCatalog.Matches)
        {
            if (fixture.Status != "FINISHED" ||
                !matchesByExt.TryGetValue(fixture.ExternalId, out var match) ||
                matchesWithLoads.Contains(match.Id))
            {
                continue;
            }

            foreach (var teamRef in new[] { fixture.Home, fixture.Away })
            {
                if (!teamsByExt.TryGetValue(teamRef.ExternalId, out var team))
                {
                    continue;
                }

                var random = new Random((fixture.ExternalId * 97) + teamRef.ExternalId);
                for (var i = 1; i <= PlayersPerTeam; i++)
                {
                    db.PlayerMatchLoads.Add(new PlayerMatchLoad
                    {
                        MatchId = match.Id,
                        TeamId = team.Id,
                        PlayerName = $"{teamRef.ShortName} Player {i}",
                        DistanceKm = Math.Round(9.0 + (random.NextDouble() * 3.5), 2),
                        Sprints = random.Next(15, 41),
                        HighIntensityMinutes = Math.Round(15.0 + (random.NextDouble() * 20.0), 2),
                    });
                }
            }
        }
    }
}
