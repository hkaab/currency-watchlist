using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CurrencyWatchlist.Infrastructure.Repositories;

public class WatchlistRepository : IWatchlistRepository
{
    private readonly CurrencyWatchlistDbContext _context;

    public WatchlistRepository(CurrencyWatchlistDbContext context) => _context = context;

    public Task<Watchlist?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.Watchlists.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<Watchlist?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken) =>
        _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Watchlist>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.Watchlists
            .Include(w => w.Items)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(Watchlist watchlist) => _context.Watchlists.Add(watchlist);

    public void Remove(Watchlist watchlist) => _context.Watchlists.Remove(watchlist);
}
