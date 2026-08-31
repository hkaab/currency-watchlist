using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Services;
using CurrencyWatchlist.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.Services;

public class WatchlistItemServiceTests
{
    private readonly IWatchlistRepository _watchlists = Substitute.For<IWatchlistRepository>();
    private readonly IWatchlistItemRepository _items = Substitute.For<IWatchlistItemRepository>();
    private readonly IRateSnapshotRepository _rateSnapshots = Substitute.For<IRateSnapshotRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly WatchlistItemService _sut;

    public WatchlistItemServiceTests()
    {
        _sut = new WatchlistItemService(_watchlists, _items, _rateSnapshots, _unitOfWork, Substitute.For<ILogger<WatchlistItemService>>());
    }

    [Fact]
    public async Task AddItemAsync_throws_NotFound_when_watchlist_missing()
    {
        _watchlists.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Watchlist?)null);

        var act = () => _sut.AddItemAsync(1, new CreateWatchlistItemRequest("USD", "AUD"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddItemAsync_normalizes_currency_codes_to_uppercase()
    {
        _watchlists.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Watchlist { Id = 1 });
        WatchlistItem? captured = null;
        _items.When(i => i.Add(Arg.Any<WatchlistItem>())).Do(ci => captured = ci.Arg<WatchlistItem>());

        var result = await _sut.AddItemAsync(1, new CreateWatchlistItemRequest(" usd ", "aud"), CancellationToken.None);

        captured!.BaseCurrency.Should().Be("USD");
        captured.QuoteCurrency.Should().Be("AUD");
        result.BaseCurrency.Should().Be("USD");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItemAsync_throws_NotFound_when_missing()
    {
        _items.GetByIdInWatchlistAsync(1, 2, Arg.Any<CancellationToken>()).Returns((WatchlistItem?)null);

        var act = () => _sut.RemoveItemAsync(1, 2, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveItemAsync_removes_and_saves()
    {
        var item = new WatchlistItem { Id = 2, WatchlistId = 1 };
        _items.GetByIdInWatchlistAsync(1, 2, Arg.Any<CancellationToken>()).Returns(item);

        await _sut.RemoveItemAsync(1, 2, CancellationToken.None);

        _items.Received(1).Remove(item);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
