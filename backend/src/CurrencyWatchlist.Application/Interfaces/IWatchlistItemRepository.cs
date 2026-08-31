using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IWatchlistItemRepository
{
    Task<WatchlistItem?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WatchlistItem?> GetByIdInWatchlistAsync(int watchlistId, int itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WatchlistItem>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WatchlistItem>> GetByWatchlistIdAsync(int watchlistId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WatchlistItem>> GetByCurrencyPairAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);
    void Add(WatchlistItem item);
    void Remove(WatchlistItem item);
}
