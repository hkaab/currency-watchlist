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

    public async Task<IReadOnlyList<WatchlistItem>> GetByCurrencyPairsAsync(
        IReadOnlyCollection<(string BaseCurrency, string QuoteCurrency)> pairs, CancellationToken cancellationToken)
    {
        if (pairs.Count == 0)
        {
            return [];
        }

        // SQLite/EF Core can't translate a tuple-list Contains() efficiently, so narrow with plain
        // IN clauses on each column (one round trip) and filter to exact pairs in memory.
        var bases = pairs.Select(p => p.BaseCurrency).Distinct().ToList();
        var quotes = pairs.Select(p => p.QuoteCurrency).Distinct().ToList();

        var candidates = await _context.WatchlistItems
            .Where(i => bases.Contains(i.BaseCurrency) && quotes.Contains(i.QuoteCurrency))
            .ToListAsync(cancellationToken);

        var pairSet = pairs.ToHashSet();
        return candidates.Where(i => pairSet.Contains((i.BaseCurrency, i.QuoteCurrency))).ToList();
    }

    public Task<bool> ExistsAsync(int watchlistId, string baseCurrency, string quoteCurrency, CancellationToken cancellationToken) =>
        _context.WatchlistItems.AnyAsync(
            i => i.WatchlistId == watchlistId && i.BaseCurrency == baseCurrency && i.QuoteCurrency == quoteCurrency,
            cancellationToken);

    public void Add(WatchlistItem item) => _context.WatchlistItems.Add(item);

    public void Remove(WatchlistItem item) => _context.WatchlistItems.Remove(item);
}
