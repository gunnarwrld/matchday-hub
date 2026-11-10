using MatchdayHub.Domain.Enums;
using MatchdayHub.Infrastructure.Aggregator;
using MatchdayHub.Infrastructure.Ingestion;
using MatchdayHub.UnitTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatchdayHub.UnitTests;

public class FixtureSyncServiceTests
{
    [Fact]
    public async Task SyncAsync_advances_complete_snapshots_but_ignores_stale_status_and_scores()
    {
        using var harness = new SqliteHarness();
        await using var db = harness.NewContext();
        var client = new MutableAggregatorClient();
        var service = new FixtureSyncService(db, client, NullLogger<FixtureSyncService>.Instance);

        client.Matches = [Fixture("LIVE", 1, 0)];
        await service.SyncAsync();

        client.Matches = [Fixture("LIVE", 2, 0)];
        await service.SyncAsync();
        var match = await db.Matches.SingleAsync();
        Assert.Equal(MatchStatus.Live, match.Status);
        Assert.Equal(2, match.HomeScore);
        Assert.Equal(0, match.AwayScore);

        client.Matches = [Fixture("LIVE", 1, 0)]; // older poll must not undo a live event
        await service.SyncAsync();
        Assert.Equal(2, match.HomeScore);
        Assert.Equal(0, match.AwayScore);

        client.Matches = [Fixture("FINISHED", 2, 0)];
        await service.SyncAsync();
        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal(2, match.HomeScore);

        client.Matches = [Fixture("LIVE", 1, 0)]; // completed state must not regress
        await service.SyncAsync();
        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal(2, match.HomeScore);
    }

    private static AggregatorMatch Fixture(string status, int homeScore, int awayScore) => new(
        ExternalId: 1001,
        CompetitionExternalId: 10,
        CompetitionName: "Test League",
        Country: "Test Country",
        Season: "2026",
        Home: new AggregatorTeamRef(20, "Home FC", "HOM", "Home Ground"),
        Away: new AggregatorTeamRef(30, "Away FC", "AWY", "Away Ground"),
        KickoffUtc: new DateTimeOffset(2026, 11, 10, 18, 0, 0, TimeSpan.Zero),
        Status: status,
        HomeScore: homeScore,
        AwayScore: awayScore);

    private sealed class MutableAggregatorClient : IAggregatorClient
    {
        public IReadOnlyList<AggregatorMatch> Matches { get; set; } = [];

        public Task<IReadOnlyList<AggregatorMatch>> GetMatchesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Matches);

        public Task<IReadOnlyList<AggregatorEvent>> GetLiveEventsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AggregatorEvent>>([]);
    }
}
