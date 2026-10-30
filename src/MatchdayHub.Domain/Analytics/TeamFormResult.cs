namespace MatchdayHub.Domain.Analytics;

/// <summary>
/// A team's recent form over a window of finished matches.
/// </summary>
/// <param name="Form">Results most-recent-first, e.g. <c>"WWDLW"</c>.</param>
public sealed record TeamFormResult(
    Guid TeamId,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    string Form);
