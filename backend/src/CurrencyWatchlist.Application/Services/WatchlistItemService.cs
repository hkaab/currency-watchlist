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

        var baseCurrency = request.BaseCurrency.Trim().ToUpperInvariant();
        var quoteCurrency = request.QuoteCurrency.Trim().ToUpperInvariant();

        if (await _items.ExistsAsync(watchlistId, baseCurrency, quoteCurrency, cancellationToken))
        {
            throw new DuplicateWatchlistItemException(baseCurrency, quoteCurrency);
        }

        var item = new WatchlistItem
        {
            WatchlistId = watchlistId,
            BaseCurrency = baseCurrency,
            QuoteCurrency = quoteCurrency,
            CreatedAt = DateTime.UtcNow
        };

        _items.Add(item);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Backstop for the race between the ExistsAsync check above and this save: a
            // concurrent request for the same pair can slip past the check and hit the
            // unique index (WatchlistId, BaseCurrency, QuoteCurrency) first. We don't depend
            // on the persistence provider's exception type here (Application has no EF Core
            // reference) - if the pair now exists, that's what happened; otherwise rethrow.
            if (await _items.ExistsAsync(watchlistId, baseCurrency, quoteCurrency, cancellationToken))
            {
                throw new DuplicateWatchlistItemException(baseCurrency, quoteCurrency);
            }

            throw;
        }

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
