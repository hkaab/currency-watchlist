using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Mappings;
using CurrencyWatchlist.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CurrencyWatchlist.Application.Services;

public sealed class WatchlistItemService : IWatchlistItemService
{
    private readonly IWatchlistRepository _watchlists;
    private readonly IWatchlistItemRepository _items;
    private readonly IRateSnapshotRepository _rateSnapshots;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WatchlistItemService> _logger;

    public WatchlistItemService(
        IWatchlistRepository watchlists,
        IWatchlistItemRepository items,
        IRateSnapshotRepository rateSnapshots,
        IUnitOfWork unitOfWork,
        ILogger<WatchlistItemService> logger)
    {
        _watchlists = watchlists;
        _items = items;
        _rateSnapshots = rateSnapshots;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WatchlistItemResponse> AddItemAsync(int watchlistId, CreateWatchlistItemRequest request, CancellationToken cancellationToken)
    {
        _ = await _watchlists.GetByIdAsync(watchlistId, cancellationToken)
            ?? throw new NotFoundException(nameof(Watchlist), watchlistId);

        var item = new WatchlistItem
        {
            WatchlistId = watchlistId,
            BaseCurrency = request.BaseCurrency.Trim().ToUpperInvariant(),
            QuoteCurrency = request.QuoteCurrency.Trim().ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        _items.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added item {ItemId} ({Base}/{Quote}) to watchlist {WatchlistId}",
            item.Id, item.BaseCurrency, item.QuoteCurrency, watchlistId);

        var latestRate = await _rateSnapshots.GetLatestAsync(item.BaseCurrency, item.QuoteCurrency, cancellationToken);
        return item.ToResponse(latestRate);
    }

    public async Task RemoveItemAsync(int watchlistId, int itemId, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdInWatchlistAsync(watchlistId, itemId, cancellationToken)
            ?? throw new NotFoundException(nameof(WatchlistItem), itemId);

        _items.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed item {ItemId} from watchlist {WatchlistId}", itemId, watchlistId);
    }
}
