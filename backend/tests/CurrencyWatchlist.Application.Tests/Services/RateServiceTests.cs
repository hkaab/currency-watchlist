using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Services;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.Services;

public class RateServiceTests
{
    private readonly IWatchlistRepository _watchlists = Substitute.For<IWatchlistRepository>();
    private readonly IWatchlistItemRepository _items = Substitute.For<IWatchlistItemRepository>();
    private readonly IRateSnapshotRepository _rateSnapshots = Substitute.For<IRateSnapshotRepository>();
    private readonly IRateProvider _rateProvider = Substitute.For<IRateProvider>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RateService _sut;

    public RateServiceTests()
    {
        _sut = new RateService(
            _watchlists, _items, _rateSnapshots, _rateProvider, _eventPublisher, _unitOfWork,
            Substitute.For<ILogger<RateService>>());
    }

    [Fact]
    public async Task RefreshAsync_throws_NotFound_for_unknown_watchlist()
    {
        _watchlists.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Watchlist?)null);

        var act = () => _sut.RefreshAsync(1, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RefreshAsync_groups_items_by_base_currency_and_persists_snapshots()
    {
        _items.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<WatchlistItem>
        {
            new() { Id = 1, WatchlistId = 1, BaseCurrency = "USD", QuoteCurrency = "AUD" },
            new() { Id = 2, WatchlistId = 1, BaseCurrency = "USD", QuoteCurrency = "EUR" }
        });
        _rateProvider.GetLatestRatesAsync("USD", Arg.Is<IReadOnlyCollection<string>>(q => q.Count == 2), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote>
            {
                new("USD", "AUD", 1.5m, DateTime.UtcNow),
                new("USD", "EUR", 0.9m, DateTime.UtcNow)
            });

        var result = await _sut.RefreshAsync(null, CancellationToken.None);

        result.RefreshedPairCount.Should().Be(2);
        _rateSnapshots.Received(1).AddRange(Arg.Is<IEnumerable<RateSnapshot>>(s => s.Count() == 2));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Any<RatesRefreshedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_returns_partial_results_when_one_base_currency_group_fails()
    {
        _items.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<WatchlistItem>
        {
            new() { Id = 1, WatchlistId = 1, BaseCurrency = "USD", QuoteCurrency = "AUD" },
            new() { Id = 2, WatchlistId = 1, BaseCurrency = "ZZZ", QuoteCurrency = "AUD" }
        });
        _rateProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote> { new("USD", "AUD", 1.5m, DateTime.UtcNow) });
        _rateProvider.GetLatestRatesAsync("ZZZ", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RateQuote>>(_ => throw new UnknownCurrencyException("ZZZ"));

        var result = await _sut.RefreshAsync(null, CancellationToken.None);

        result.RefreshedPairCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_throws_when_every_group_fails()
    {
        _items.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<WatchlistItem>
        {
            new() { Id = 1, WatchlistId = 1, BaseCurrency = "ZZZ", QuoteCurrency = "AUD" }
        });
        _rateProvider.GetLatestRatesAsync("ZZZ", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RateQuote>>(_ => throw new UnknownCurrencyException("ZZZ"));

        var act = () => _sut.RefreshAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<UnknownCurrencyException>();
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<RatesRefreshedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_scoped_to_watchlist_only_fetches_that_watchlists_items()
    {
        _watchlists.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Watchlist { Id = 1 });
        _items.GetByWatchlistIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<WatchlistItem>
        {
            new() { Id = 1, WatchlistId = 1, BaseCurrency = "USD", QuoteCurrency = "AUD" }
        });
        _rateProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote> { new("USD", "AUD", 1.5m, DateTime.UtcNow) });

        await _sut.RefreshAsync(1, CancellationToken.None);

        await _items.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLatestAsync_normalizes_case_and_throws_NotFound_when_missing()
    {
        _rateSnapshots.GetLatestAsync("USD", "AUD", Arg.Any<CancellationToken>()).Returns((RateSnapshot?)null);

        var act = () => _sut.GetLatestAsync("usd", "aud", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetLatestAsync_returns_mapped_snapshot()
    {
        _rateSnapshots.GetLatestAsync("USD", "AUD", Arg.Any<CancellationToken>())
            .Returns(new RateSnapshot { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.6m });

        var result = await _sut.GetLatestAsync("USD", "AUD", CancellationToken.None);

        result.Rate.Should().Be(1.6m);
    }

    [Fact]
    public async Task GetHistoryAsync_returns_mapped_snapshots()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        _rateSnapshots.GetHistoryAsync("USD", "AUD", from, to, Arg.Any<CancellationToken>())
            .Returns(new List<RateSnapshot>
            {
                new() { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m },
                new() { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.55m }
            });

        var result = await _sut.GetHistoryAsync("USD", "AUD", from, to, CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
