using CurrencyWatchlist.Application.Dtos.Items;

namespace CurrencyWatchlist.Application.Services;

public interface IWatchlistItemService
{
    Task<WatchlistItemResponse> AddItemAsync(int watchlistId, CreateWatchlistItemRequest request, CancellationToken cancellationToken);
    Task RemoveItemAsync(int watchlistId, int itemId, CancellationToken cancellationToken);
}
