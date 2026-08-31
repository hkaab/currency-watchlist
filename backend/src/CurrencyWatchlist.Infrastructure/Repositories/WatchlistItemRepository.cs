using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CurrencyWatchlist.Infrastructure.Repositories;

public class WatchlistItemRepository : IWatchlistItemRepository
{
    private readonly CurrencyWatchlistDbContext _context;

    public WatchlistItemRepository(CurrencyWatchlistDbContext context) => _context = context;

    public Task<WatchlistItem?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.WatchlistItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<WatchlistItem?> GetByIdInWatchlistAsync(int watchlistId, int itemId, CancellationToken cancellationToken) =>
        _context.WatchlistItems.FirstOrDefaultAsync(i => i.Id == itemId && i.WatchlistId == watchlistId, cancellationToken);

    public async Task<IReadOnlyList<WatchlistItem>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.WatchlistItems.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WatchlistItem>> GetByWatchlistIdAsync(int watchlistId, CancellationToken cancellationToken) =>
        await _context.WatchlistItems.Where(i => i.WatchlistId == watchlistId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WatchlistItem>> GetByCurrencyPairAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken) =>
        await _context.WatchlistItems
            .Where(i => i.BaseCurrency == baseCurrency && i.QuoteCurrency == quoteCurrency)
            .ToListAsync(cancellationToken);

    public void Add(WatchlistItem item) => _context.WatchlistItems.Add(item);

    public void Remove(WatchlistItem item) => _context.WatchlistItems.Remove(item);
}
