using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Domain.Events;

/// <summary>Published whenever an alert rule's condition is met and an AlertEvent has been persisted.</summary>
public sealed class AlertTriggeredEvent : IDomainEvent
{
    public AlertTriggeredEvent(AlertEvent alertEvent, int watchlistId, string baseCurrency, string quoteCurrency)
    {
        AlertEvent = alertEvent;
        WatchlistId = watchlistId;
        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
    }

    public AlertEvent AlertEvent { get; }
    public int WatchlistId { get; }
    public string BaseCurrency { get; }
    public string QuoteCurrency { get; }
}
