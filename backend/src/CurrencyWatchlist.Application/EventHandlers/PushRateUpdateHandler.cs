using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Events;

namespace CurrencyWatchlist.Application.EventHandlers;

/// <summary>Pushes newly refreshed rates to every watchlist whose items reference the affected pairs.</summary>
public sealed class PushRateUpdateHandler : IDomainEventHandler<RatesRefreshedEvent>
{
    private readonly IWatchlistItemRepository _items;
    private readonly IRealtimeNotifier _notifier;

    public PushRateUpdateHandler(IWatchlistItemRepository items, IRealtimeNotifier notifier)
    {
        _items = items;
        _notifier = notifier;
    }

    public async Task HandleAsync(RatesRefreshedEvent domainEvent, CancellationToken cancellationToken)
    {
        var snapshotsByWatchlist = new Dictionary<int, List<RateSnapshot>>();

        foreach (var snapshot in domainEvent.Snapshots)
        {
            var items = await _items.GetByCurrencyPairAsync(snapshot.BaseCurrency, snapshot.QuoteCurrency, cancellationToken);

            foreach (var watchlistId in items.Select(i => i.WatchlistId).Distinct())
            {
                if (!snapshotsByWatchlist.TryGetValue(watchlistId, out var list))
                {
                    list = [];
                    snapshotsByWatchlist[watchlistId] = list;
                }

                list.Add(snapshot);
            }
        }

        foreach (var (watchlistId, snapshots) in snapshotsByWatchlist)
        {
            await _notifier.NotifyRatesUpdatedAsync(watchlistId, snapshots, cancellationToken);
        }
    }
}
