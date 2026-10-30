using Microsoft.AspNetCore.SignalR;

namespace MatchdayHub.Infrastructure.Realtime;

/// <summary>
/// SignalR hub for live match updates — the .NET counterpart of the aggregator's
/// Socket.io gateway. Clients join a <c>match:{id}</c> group to receive only the events
/// for the match they are watching (SignalR "groups" == Socket.io "rooms").
/// </summary>
public sealed class MatchHub : Hub
{
    public static string GroupFor(Guid matchId) => $"match:{matchId}";

    /// <summary>Subscribe the caller to a single match's event stream.</summary>
    public Task SubscribeToMatch(Guid matchId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(matchId));

    /// <summary>Unsubscribe the caller from a match's event stream.</summary>
    public Task UnsubscribeFromMatch(Guid matchId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(matchId));
}
