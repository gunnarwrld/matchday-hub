using MatchdayHub.Domain.Analytics;
using MatchdayHub.Domain.Entities;

namespace MatchdayHub.UnitTests;

public class PlayerLoadAggregatorTests
{
    private static readonly Guid MatchId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TeamId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static PlayerMatchLoad Load(string name, double distance, int sprints, double high) => new()
    {
        MatchId = MatchId,
        TeamId = TeamId,
        PlayerName = name,
        DistanceKm = distance,
        Sprints = sprints,
        HighIntensityMinutes = high,
    };

    [Fact]
    public void Aggregates_totals_and_average_distance()
    {
        var loads = new[]
        {
            Load("A", 10.0, 20, 25.0),
            Load("B", 11.0, 30, 30.0),
            Load("C", 9.0, 10, 20.0),
        };

        var report = PlayerLoadAggregator.Summarise(MatchId, loads);

        Assert.Equal(3, report.PlayerCount);
        Assert.Equal(30.0, report.TotalDistanceKm);
        Assert.Equal(10.0, report.AverageDistanceKm);
        Assert.Equal(60, report.TotalSprints);
        Assert.Equal(75.0, report.TotalHighIntensityMinutes);
    }

    [Fact]
    public void Orders_players_by_distance_descending_and_flags_the_top()
    {
        var loads = new[]
        {
            Load("A", 10.0, 20, 25.0),
            Load("B", 12.5, 30, 30.0),
            Load("C", 9.0, 10, 20.0),
        };

        var report = PlayerLoadAggregator.Summarise(MatchId, loads);

        Assert.Equal("B", report.TopDistancePlayer!.PlayerName);
        Assert.Equal(["B", "A", "C"], report.Players.Select(p => p.PlayerName).ToArray());
    }

    [Fact]
    public void Rounds_to_two_decimal_places()
    {
        var loads = new[]
        {
            Load("A", 10.123, 1, 5.555),
            Load("B", 10.123, 1, 5.555),
        };

        var report = PlayerLoadAggregator.Summarise(MatchId, loads);

        Assert.Equal(10.12, report.Players[0].DistanceKm);
        Assert.Equal(20.24, report.TotalDistanceKm); // sum of the per-player rounded values
        Assert.Equal(10.12, report.AverageDistanceKm);
    }

    [Fact]
    public void Empty_input_yields_a_zeroed_report_with_no_top_player()
    {
        var report = PlayerLoadAggregator.Summarise(MatchId, []);

        Assert.Equal(0, report.PlayerCount);
        Assert.Equal(0, report.TotalDistanceKm);
        Assert.Equal(0, report.AverageDistanceKm);
        Assert.Null(report.TopDistancePlayer);
        Assert.Empty(report.Players);
    }
}
