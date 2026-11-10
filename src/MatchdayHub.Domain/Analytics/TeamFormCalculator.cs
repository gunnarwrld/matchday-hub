using MatchdayHub.Domain.Entities;
using MatchdayHub.Domain.Enums;

namespace MatchdayHub.Domain.Analytics;

/// <summary>
/// Pure, dependency-free computation of a team's recent form. Kept in the Domain
/// layer so it can be unit-tested without a database (Node analogy: a pure function
/// in a <c>lib/</c> module, not a service that touches Prisma).
/// </summary>
public static class TeamFormCalculator
{
    public const int DefaultWindow = 5;

    /// <summary>
    /// Computes form for <paramref name="teamId"/> from the most recent
    /// <paramref name="window"/> <b>finished</b> matches it appears in.
    /// </summary>
    public static TeamFormResult Calculate(Guid teamId, IEnumerable<Match> matches, int window = DefaultWindow)
    {
        ArgumentNullException.ThrowIfNull(matches);
        if (window < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(window), window, "Window must be at least 1.");
        }

        var recent = matches
            .Where(m => m.Status == MatchStatus.Finished &&
                        (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
            .OrderByDescending(m => m.KickoffUtc)
            .Take(window)
            .ToList();

        int wins = 0, draws = 0, losses = 0, goalsFor = 0, goalsAgainst = 0;
        var form = new char[recent.Count];

        for (var i = 0; i < recent.Count; i++)
        {
            var match = recent[i];
            var isHome = match.HomeTeamId == teamId;
            var scored = isHome ? match.HomeScore : match.AwayScore;
            var conceded = isHome ? match.AwayScore : match.HomeScore;

            goalsFor += scored;
            goalsAgainst += conceded;

            if (scored > conceded)
            {
                wins++;
                form[i] = 'W';
            }
            else if (scored < conceded)
            {
                losses++;
                form[i] = 'L';
            }
            else
            {
                draws++;
                form[i] = 'D';
            }
        }

        return new TeamFormResult(
            TeamId: teamId,
            Played: recent.Count,
            Wins: wins,
            Draws: draws,
            Losses: losses,
            GoalsFor: goalsFor,
            GoalsAgainst: goalsAgainst,
            GoalDifference: goalsFor - goalsAgainst,
            Points: (wins * 3) + draws,
            Form: new string(form));
    }
}
