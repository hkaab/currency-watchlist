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
        var pairs = domainEvent.Snapshots.Select(s => (s.BaseCurrency, s.QuoteCurrency)).Distinct().ToList();
        var items = await _items.GetByCurrencyPairsAsync(pairs, cancellationToken);
        var watchlistIdsByPair = items
            .GroupBy(i => (i.BaseCurrency, i.QuoteCurrency))
            .ToDictionary(g => g.Key, g => g.Select(i => i.WatchlistId).Distinct().ToList());

        var snapshotsByWatchlist = new Dictionary<int, List<RateSnapshot>>();

        foreach (var snapshot in domainEvent.Snapshots)
        {
            if (!watchlistIdsByPair.TryGetValue((snapshot.BaseCurrency, snapshot.QuoteCurrency), out var watchlistIds))
            {
                continue;
            }

            foreach (var watchlistId in watchlistIds)
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
