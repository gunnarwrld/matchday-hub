using Microsoft.AspNetCore.SignalR;

namespace MatchdayHub.Infrastructure.Realtime;

/// <summary>
/// Broadcasts match updates to the <c>match:{id}</c> group via SignalR. When a Redis
/// backplane is configured, this works identically whether the broadcast originates in
/// the API process or the standalone Worker.
/// </summary>
public sealed class SignalRMatchNotifier(IHubContext<MatchHub> hubContext) : IMatchNotifier
{
    public Task MatchEventAsync(MatchEventBroadcast broadcast, CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(MatchHub.GroupFor(broadcast.MatchId))
            .SendAsync(MatchHubMethods.MatchEvent, broadcast, cancellationToken);

    public Task ScoreUpdateAsync(ScoreUpdateBroadcast broadcast, CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(MatchHub.GroupFor(broadcast.MatchId))
            .SendAsync(MatchHubMethods.ScoreUpdate, broadcast, cancellationToken);
}
