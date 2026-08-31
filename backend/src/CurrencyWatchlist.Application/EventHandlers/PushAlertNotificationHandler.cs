using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Domain.Events;

namespace CurrencyWatchlist.Application.EventHandlers;

/// <summary>Pushes a triggered alert to the watchlist it belongs to.</summary>
public sealed class PushAlertNotificationHandler : IDomainEventHandler<AlertTriggeredEvent>
{
    private readonly IRealtimeNotifier _notifier;

    public PushAlertNotificationHandler(IRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task HandleAsync(AlertTriggeredEvent domainEvent, CancellationToken cancellationToken) =>
        _notifier.NotifyAlertTriggeredAsync(domainEvent.WatchlistId, domainEvent.AlertEvent, cancellationToken);
}
