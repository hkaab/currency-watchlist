using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IWatchlistRepository
{
    Task<Watchlist?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Watchlist?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Watchlist>> GetAllAsync(CancellationToken cancellationToken);
    void Add(Watchlist watchlist);
    void Remove(Watchlist watchlist);
}
