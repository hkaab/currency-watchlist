using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IWatchlistItemRepository
{
    Task<WatchlistItem?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WatchlistItem?> GetByIdInWatchlistAsync(int watchlistId, int itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WatchlistItem>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WatchlistItem>> GetByWatchlistIdAsync(int watchlistId, CancellationToken cancellationToken);
    /// <summary>Items across all watchlists matching any of the given pairs, in one round trip.</summary>
    Task<IReadOnlyList<WatchlistItem>> GetByCurrencyPairsAsync(
        IReadOnlyCollection<(string BaseCurrency, string QuoteCurrency)> pairs, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int watchlistId, string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);
    void Add(WatchlistItem item);
    void Remove(WatchlistItem item);
}
