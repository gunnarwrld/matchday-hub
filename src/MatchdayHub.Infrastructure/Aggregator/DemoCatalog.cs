namespace MatchdayHub.Infrastructure.Aggregator;

/// <summary>
/// The single source of demo data, shared by <see cref="FakeAggregatorClient"/> and
/// the database seeder. A fixed Primeira Liga season with several finished matches
/// (so team-form and player-load endpoints return something real) plus one live match
/// whose timeline the fake reveals over time to drive the SignalR stream.
/// </summary>
public static class DemoCatalog
{
    public const int CompetitionExternalId = 100;
    public const string CompetitionName = "Primeira Liga";
    public const string Country = "Portugal";
    public const string Season = "2025/2026";
    public const int LiveMatchExternalId = 9001;

    public static IReadOnlyList<AggregatorTeamRef> Teams { get; } =
    [
        new(1, "FC Porto", "POR", "Estádio do Dragão"),
        new(2, "SL Benfica", "BEN", "Estádio da Luz"),
        new(3, "Sporting CP", "SCP", "Estádio José Alvalade"),
        new(4, "SC Braga", "BRA", "Estádio Municipal de Braga"),
        new(5, "Vitória SC", "VSC", "Estádio D. Afonso Henriques"),
        new(6, "Boavista FC", "BOA", "Estádio do Bessa"),
    ];

    public static IReadOnlyList<AggregatorMatch> Matches { get; }

    /// <summary>Goal events for the finished matches (used to seed timelines).</summary>
    public static IReadOnlyList<AggregatorEvent> FinishedEvents { get; }

    /// <summary>Ordered timeline the fake reveals for the live match.</summary>
    public static IReadOnlyList<AggregatorEvent> LiveEventScript { get; }

    static DemoCatalog()
    {
        var teamsById = Teams.ToDictionary(t => t.ExternalId);
        var now = DateTimeOffset.UtcNow;

        // (homeExternalId, awayExternalId, daysAgo, homeScore, awayScore)
        (int Home, int Away, int DaysAgo, int HomeScore, int AwayScore)[] finished =
        [
            (1, 3, 21, 2, 1),
            (2, 4, 20, 1, 1),
            (3, 5, 14, 3, 0),
            (4, 1, 13, 0, 2),
            (5, 6, 10, 2, 2),
            (6, 2, 7, 0, 3),
            (1, 5, 6, 4, 0),
            (3, 2, 3, 1, 2),
        ];

        var matches = new List<AggregatorMatch>();
        var finishedEvents = new List<AggregatorEvent>();
        var matchExternalId = 8001;

        foreach (var f in finished)
        {
            var home = teamsById[f.Home];
            var away = teamsById[f.Away];
            matches.Add(new AggregatorMatch(
                ExternalId: matchExternalId,
                CompetitionExternalId: CompetitionExternalId,
                CompetitionName: CompetitionName,
                Country: Country,
                Season: Season,
                Home: home,
                Away: away,
                KickoffUtc: now.AddDays(-f.DaysAgo),
                Status: "FINISHED",
                HomeScore: f.HomeScore,
                AwayScore: f.AwayScore));

            finishedEvents.AddRange(BuildGoalEvents(matchExternalId, home, away, f.HomeScore, f.AwayScore));
            matchExternalId++;
        }

        // The live match: starts goalless, animated by the fake client.
        matches.Add(new AggregatorMatch(
            ExternalId: LiveMatchExternalId,
            CompetitionExternalId: CompetitionExternalId,
            CompetitionName: CompetitionName,
            Country: Country,
            Season: Season,
            Home: teamsById[1],
            Away: teamsById[2],
            KickoffUtc: now.AddMinutes(-5),
            Status: "LIVE",
            HomeScore: 0,
            AwayScore: 0));

        Matches = matches;
        FinishedEvents = finishedEvents;
        LiveEventScript = BuildLiveScript(teamsById[1], teamsById[2]);
    }

    private static IEnumerable<AggregatorEvent> BuildGoalEvents(
        int matchExternalId, AggregatorTeamRef home, AggregatorTeamRef away, int homeScore, int awayScore)
    {
        var index = 0;
        var minute = 8;
        for (var i = 0; i < homeScore; i++)
        {
            yield return new AggregatorEvent(
                $"demo-evt-{matchExternalId}-{index++}", matchExternalId, home.ExternalId,
                $"{home.ShortName} Forward {i + 1}", "GOAL", minute, homeScore, awayScore, "FINISHED");
            minute += 11;
        }

        minute = 14;
        for (var i = 0; i < awayScore; i++)
        {
            yield return new AggregatorEvent(
                $"demo-evt-{matchExternalId}-{index++}", matchExternalId, away.ExternalId,
                $"{away.ShortName} Forward {i + 1}", "GOAL", minute, homeScore, awayScore, "FINISHED");
            minute += 13;
        }
    }

    private static IReadOnlyList<AggregatorEvent> BuildLiveScript(AggregatorTeamRef home, AggregatorTeamRef away)
    {
        // (minute, teamIsHome, type, player)
        (int Minute, bool Home, string Type, string Player)[] script =
        [
            (9, true, "GOAL", "POR Striker"),
            (21, false, "YELLOW_CARD", "BEN Defender"),
            (34, false, "GOAL", "BEN Winger"),
            (45, true, "YELLOW_CARD", "POR Midfielder"),
            (52, true, "GOAL", "POR Striker"),
            (63, true, "SUBSTITUTION", "POR Sub"),
            (71, false, "RED_CARD", "BEN Defender"),
            (80, true, "GOAL", "POR Winger"),
            (88, false, "GOAL", "BEN Forward"),
        ];

        var events = new List<AggregatorEvent>(script.Length);
        int homeScore = 0, awayScore = 0;
        var index = 0;
        foreach (var s in script)
        {
            if (s.Type == "GOAL")
            {
                if (s.Home)
                {
                    homeScore++;
                }
                else
                {
                    awayScore++;
                }
            }

            var team = s.Home ? home : away;
            var status = index == script.Length - 1 ? "FINISHED" : "LIVE";
            events.Add(new AggregatorEvent(
                $"demo-live-{LiveMatchExternalId}-{index}", LiveMatchExternalId, team.ExternalId,
                s.Player, s.Type, s.Minute, homeScore, awayScore, status));
            index++;
        }

        return events;
    }
}
