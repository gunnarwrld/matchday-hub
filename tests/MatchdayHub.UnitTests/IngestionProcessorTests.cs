using MatchdayHub.Domain.Entities;
using MatchdayHub.Domain.Enums;
using MatchdayHub.Infrastructure.Ingestion;
using MatchdayHub.Infrastructure.Realtime;
using MatchdayHub.UnitTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatchdayHub.UnitTests;

public class IngestionProcessorTests
{
    private const int MatchExternalId = 9001;
    private const int HomeExternalId = 1;
    private const int AwayExternalId = 2;

    private sealed class CountingNotifier : IMatchNotifier
    {
        public int EventBroadcasts { get; private set; }

        public int ScoreBroadcasts { get; private set; }

        public Task MatchEventAsync(MatchEventBroadcast broadcast, CancellationToken cancellationToken = default)
        {
            EventBroadcasts++;
            return Task.CompletedTask;
        }

        public Task ScoreUpdateAsync(ScoreUpdateBroadcast broadcast, CancellationToken cancellationToken = default)
        {
            ScoreBroadcasts++;
            return Task.CompletedTask;
        }
    }

    private static async Task SeedFixtureAsync(SqliteHarness harness)
    {
        await using var db = harness.NewContext();
        var competition = new Competition { ExternalId = 100, Name = "Primeira Liga", Country = "Portugal", Season = "2025/2026" };
        var home = new Team { ExternalId = HomeExternalId, Name = "FC Porto", ShortName = "POR", Stadium = "Dragão" };
        var away = new Team { ExternalId = AwayExternalId, Name = "SL Benfica", ShortName = "BEN", Stadium = "Luz" };
        db.AddRange(competition, home, away);
        db.Matches.Add(new Match
        {
            ExternalId = MatchExternalId,
            Competition = competition,
            HomeTeam = home,
            AwayTeam = away,
            KickoffUtc = DateTimeOffset.UtcNow,
            Status = MatchStatus.Live,
        });
        await db.SaveChangesAsync();
    }

    private static LiveEventMessage Goal(string id, int home, int away) => new(
        MessageId: id,
        MatchExternalId: MatchExternalId,
        TeamExternalId: HomeExternalId,
        PlayerName: "POR Striker",
        Type: MatchEventType.Goal,
        Minute: 23,
        HomeScore: home,
        AwayScore: away,
        MatchStatus: MatchStatus.Live);

    private static IngestionProcessor NewProcessor(SqliteHarness harness, IMatchNotifier notifier) =>
        new(harness.NewContext(), notifier, NullLogger<IngestionProcessor>.Instance);

    [Fact]
    public async Task First_delivery_persists_the_event_and_updates_the_score()
    {
        using var harness = new SqliteHarness();
        await SeedFixtureAsync(harness);
        var notifier = new CountingNotifier();

        var result = await NewProcessor(harness, notifier).ProcessAsync(Goal("evt-1", 1, 0));

        Assert.Equal(IngestionResult.Processed, result);

        await using var db = harness.NewContext();
        Assert.Equal(1, await db.MatchEvents.CountAsync());
        Assert.Equal(1, await db.ProcessedMessages.CountAsync());
        var match = await db.Matches.SingleAsync();
        Assert.Equal(1, match.HomeScore);
        Assert.Equal(0, match.AwayScore);
        Assert.Equal(1, notifier.EventBroadcasts);
        Assert.Equal(1, notifier.ScoreBroadcasts);
    }

    [Fact]
    public async Task Redelivery_of_the_same_event_is_a_no_op()
    {
        using var harness = new SqliteHarness();
        await SeedFixtureAsync(harness);
        var notifier = new CountingNotifier();

        var first = await NewProcessor(harness, notifier).ProcessAsync(Goal("evt-1", 1, 0));
        var second = await NewProcessor(harness, notifier).ProcessAsync(Goal("evt-1", 1, 0));

        Assert.Equal(IngestionResult.Processed, first);
        Assert.Equal(IngestionResult.Skipped, second);

        await using var db = harness.NewContext();
        Assert.Equal(1, await db.MatchEvents.CountAsync());
        Assert.Equal(1, await db.ProcessedMessages.CountAsync());
        Assert.Equal(1, notifier.EventBroadcasts); // not re-broadcast
    }

    [Fact]
    public async Task Distinct_events_each_persist_and_advance_the_score()
    {
        using var harness = new SqliteHarness();
        await SeedFixtureAsync(harness);
        var notifier = new CountingNotifier();

        await NewProcessor(harness, notifier).ProcessAsync(Goal("evt-1", 1, 0));
        await NewProcessor(harness, notifier).ProcessAsync(Goal("evt-2", 2, 0));

        await using var db = harness.NewContext();
        Assert.Equal(2, await db.MatchEvents.CountAsync());
        Assert.Equal(2, notifier.EventBroadcasts);
        Assert.Equal(2, (await db.Matches.SingleAsync()).HomeScore);
    }

    [Fact]
    public async Task Unknown_match_is_reported_as_unresolved_and_writes_nothing()
    {
        using var harness = new SqliteHarness();
        await SeedFixtureAsync(harness);
        var notifier = new CountingNotifier();

        var message = Goal("evt-1", 1, 0) with { MatchExternalId = 404404 };
        var result = await NewProcessor(harness, notifier).ProcessAsync(message);

        Assert.Equal(IngestionResult.Unresolved, result);

        await using var db = harness.NewContext();
        Assert.Equal(0, await db.MatchEvents.CountAsync());
        Assert.Equal(0, await db.ProcessedMessages.CountAsync());
        Assert.Equal(0, notifier.EventBroadcasts);
    }
}
