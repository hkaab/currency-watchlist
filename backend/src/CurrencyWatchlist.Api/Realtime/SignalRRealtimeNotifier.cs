using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Mappings;
using CurrencyWatchlist.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace CurrencyWatchlist.Api.Realtime;

/// <summary>Web-layer implementation of <see cref="IRealtimeNotifier"/> backed by SignalR.</summary>
public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyRatesUpdatedAsync(int watchlistId, IReadOnlyList<RateSnapshot> snapshots, CancellationToken cancellationToken) =>
        _hubContext.Clients
            .Group(NotificationsHub.GroupName(watchlistId))
            .SendAsync("RatesUpdated", snapshots.Select(s => s.ToResponse()).ToList(), cancellationToken);

    public Task NotifyAlertTriggeredAsync(int watchlistId, AlertEvent alertEvent, CancellationToken cancellationToken) =>
        _hubContext.Clients
            .Group(NotificationsHub.GroupName(watchlistId))
            .SendAsync("AlertTriggered", new
            {
                alertEvent.Id,
                alertEvent.AlertRuleId,
                alertEvent.TriggeredAt,
                alertEvent.Rate,
                alertEvent.Message
            }, cancellationToken);
}
