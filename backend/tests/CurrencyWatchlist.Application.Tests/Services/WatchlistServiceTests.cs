using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Watchlists;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Services;
using CurrencyWatchlist.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.Services;

public class WatchlistServiceTests
{
    private readonly IWatchlistRepository _watchlists = Substitute.For<IWatchlistRepository>();
    private readonly IRateSnapshotRepository _rateSnapshots = Substitute.For<IRateSnapshotRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly WatchlistService _sut;

    public WatchlistServiceTests()
    {
        _sut = new WatchlistService(_watchlists, _rateSnapshots, _unitOfWork, Substitute.For<ILogger<WatchlistService>>());
    }

    [Fact]
    public async Task CreateAsync_persists_and_returns_response()
    {
        Watchlist? captured = null;
        _watchlists.When(w => w.Add(Arg.Any<Watchlist>())).Do(ci => captured = ci.Arg<Watchlist>());

        var result = await _sut.CreateAsync(new CreateWatchlistRequest("  My List  "), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("My List");
        result.Name.Should().Be("My List");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_maps_every_watchlist()
    {
        _watchlists.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Watchlist>
        {
            new() { Id = 1, Name = "A", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "B", CreatedAt = DateTime.UtcNow }
        });

        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFound_when_missing()
    {
        _watchlists.GetByIdWithItemsAsync(42, Arg.Any<CancellationToken>()).Returns((Watchlist?)null);

        var act = () => _sut.GetByIdAsync(42, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_includes_latest_rate_per_item()
    {
        var watchlist = new Watchlist
        {
            Id = 1,
            Name = "List",
            CreatedAt = DateTime.UtcNow,
            Items = new List<WatchlistItem>
            {
                new() { Id = 10, WatchlistId = 1, BaseCurrency = "USD", QuoteCurrency = "AUD" }
            }
        };
        _watchlists.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(watchlist);
        var snapshot = new RateSnapshot { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m };
        _rateSnapshots.GetLatestAsync("USD", "AUD", Arg.Any<CancellationToken>()).Returns(snapshot);

        var result = await _sut.GetByIdAsync(1, CancellationToken.None);

        result.Items.Single().LatestRate.Should().NotBeNull();
        result.Items.Single().LatestRate!.Rate.Should().Be(1.5m);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFound_when_missing()
    {
        _watchlists.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Watchlist?)null);

        var act = () => _sut.DeleteAsync(1, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves()
    {
        var watchlist = new Watchlist { Id = 1, Name = "List" };
        _watchlists.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(watchlist);

        await _sut.DeleteAsync(1, CancellationToken.None);

        _watchlists.Received(1).Remove(watchlist);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
