using CurrencyWatchlist.Application.EventHandlers;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Events;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.EventHandlers;

public class PushRateUpdateHandlerTests
{
    private readonly IWatchlistItemRepository _items = Substitute.For<IWatchlistItemRepository>();
    private readonly IRealtimeNotifier _notifier = Substitute.For<IRealtimeNotifier>();
    private readonly PushRateUpdateHandler _sut;

    public PushRateUpdateHandlerTests()
    {
        _sut = new PushRateUpdateHandler(_items, _notifier);
    }

    [Fact]
    public async Task Notifies_every_watchlist_referencing_the_refreshed_pair()
    {
        _items.GetByCurrencyPairsAsync(
                Arg.Is<IReadOnlyCollection<(string, string)>>(p => p.Any(x => x.Item1 == "USD" && x.Item2 == "AUD")),
                Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistItem>
            {
                new() { BaseCurrency = "USD", QuoteCurrency = "AUD", WatchlistId = 1 },
                new() { BaseCurrency = "USD", QuoteCurrency = "AUD", WatchlistId = 2 }
            });

        var snapshot = new RateSnapshot { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m };
        await _sut.HandleAsync(new RatesRefreshedEvent([snapshot]), CancellationToken.None);

        await _notifier.Received(1).NotifyRatesUpdatedAsync(1, Arg.Any<IReadOnlyList<RateSnapshot>>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyRatesUpdatedAsync(2, Arg.Any<IReadOnlyList<RateSnapshot>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_watchlists_reference_pair_means_no_notification()
    {
        _items.GetByCurrencyPairsAsync(Arg.Any<IReadOnlyCollection<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistItem>());

        var snapshot = new RateSnapshot { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m };
        await _sut.HandleAsync(new RatesRefreshedEvent([snapshot]), CancellationToken.None);

        await _notifier.DidNotReceive().NotifyRatesUpdatedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<RateSnapshot>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fetches_items_in_a_single_batch_call_regardless_of_snapshot_count()
    {
        _items.GetByCurrencyPairsAsync(Arg.Any<IReadOnlyCollection<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistItem>
            {
                new() { BaseCurrency = "USD", QuoteCurrency = "AUD", WatchlistId = 1 },
                new() { BaseCurrency = "EUR", QuoteCurrency = "GBP", WatchlistId = 2 }
            });

        var snapshots = new List<RateSnapshot>
        {
            new() { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m },
            new() { BaseCurrency = "EUR", QuoteCurrency = "GBP", Rate = 0.85m }
        };
        await _sut.HandleAsync(new RatesRefreshedEvent(snapshots), CancellationToken.None);

        await _items.Received(1).GetByCurrencyPairsAsync(Arg.Any<IReadOnlyCollection<(string, string)>>(), Arg.Any<CancellationToken>());
    }
}
