using Microsoft.AspNetCore.SignalR;

namespace CurrencyWatchlist.Api.Realtime;

/// <summary>
/// Clients join the group for the watchlist they're currently viewing and receive
/// "RatesUpdated" / "AlertTriggered" pushes for it without polling.
/// </summary>
public class NotificationsHub : Hub
{
    public Task JoinWatchlist(int watchlistId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(watchlistId));

    public Task LeaveWatchlist(int watchlistId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(watchlistId));

    public static string GroupName(int watchlistId) => $"watchlist-{watchlistId}";
}
