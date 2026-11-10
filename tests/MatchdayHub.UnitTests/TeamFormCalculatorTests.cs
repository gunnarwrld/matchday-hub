using MatchdayHub.Domain.Analytics;
using MatchdayHub.Domain.Entities;
using MatchdayHub.Domain.Enums;

namespace MatchdayHub.UnitTests;

public class TeamFormCalculatorTests
{
    private static readonly Guid TeamA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TeamB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Match Finished(Guid home, Guid away, int homeScore, int awayScore, int daysAgo) => new()
    {
        HomeTeamId = home,
        AwayTeamId = away,
        HomeScore = homeScore,
        AwayScore = awayScore,
        Status = MatchStatus.Finished,
        KickoffUtc = DateTimeOffset.UtcNow.AddDays(-daysAgo),
    };

    [Fact]
    public void Computes_points_record_and_goals_from_both_home_and_away_games()
    {
        var matches = new[]
        {
            Finished(TeamA, TeamB, 2, 0, 5), // win
            Finished(TeamB, TeamA, 1, 1, 4), // draw (A away)
            Finished(TeamA, TeamB, 0, 3, 3), // loss
        };

        var form = TeamFormCalculator.Calculate(TeamA, matches);

        Assert.Equal(3, form.Played);
        Assert.Equal(1, form.Wins);
        Assert.Equal(1, form.Draws);
        Assert.Equal(1, form.Losses);
        Assert.Equal(3, form.GoalsFor);   // 2 + 1 + 0
        Assert.Equal(4, form.GoalsAgainst); // 0 + 1 + 3
        Assert.Equal(-1, form.GoalDifference);
        Assert.Equal(4, form.Points); // 3 + 1 + 0
    }

    [Fact]
    public void Form_string_is_most_recent_first()
    {
        var matches = new[]
        {
            Finished(TeamA, TeamB, 3, 0, 10), // oldest: W
            Finished(TeamA, TeamB, 0, 1, 5),  // L
            Finished(TeamB, TeamA, 2, 2, 1),  // newest: D
        };

        var form = TeamFormCalculator.Calculate(TeamA, matches);

        Assert.Equal("DLW", form.Form);
    }

    [Fact]
    public void Only_counts_the_most_recent_matches_within_the_window()
    {
        var matches = Enumerable.Range(1, 8)
            .Select(i => Finished(TeamA, TeamB, 1, 0, i)) // 8 wins, newest has smallest daysAgo
            .ToArray();

        var form = TeamFormCalculator.Calculate(TeamA, matches, window: 5);

        Assert.Equal(5, form.Played);
        Assert.Equal(5, form.Wins);
        Assert.Equal("WWWWW", form.Form);
    }

    [Fact]
    public void Ignores_unfinished_matches_and_other_teams()
    {
        var other = Guid.NewGuid();
        var matches = new[]
        {
            Finished(TeamA, TeamB, 1, 0, 2),
            new Match { HomeTeamId = TeamA, AwayTeamId = TeamB, Status = MatchStatus.Live, HomeScore = 5, AwayScore = 0 },
            Finished(other, TeamB, 4, 0, 1),
        };

        var form = TeamFormCalculator.Calculate(TeamA, matches);

        Assert.Equal(1, form.Played);
        Assert.Equal("W", form.Form);
    }

    [Fact]
    public void Empty_history_yields_an_empty_form()
    {
        var form = TeamFormCalculator.Calculate(TeamA, []);

        Assert.Equal(0, form.Played);
        Assert.Equal(0, form.Points);
        Assert.Equal(string.Empty, form.Form);
    }

    [Fact]
    public void Rejects_a_non_positive_window()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TeamFormCalculator.Calculate(TeamA, [], window: 0));
    }
}
