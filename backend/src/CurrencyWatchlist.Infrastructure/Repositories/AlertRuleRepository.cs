using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CurrencyWatchlist.Infrastructure.Repositories;

public class AlertRuleRepository : IAlertRuleRepository
{
    private readonly CurrencyWatchlistDbContext _context;

    public AlertRuleRepository(CurrencyWatchlistDbContext context) => _context = context;

    public Task<AlertRule?> GetByIdWithItemAsync(int id, CancellationToken cancellationToken) =>
        _context.AlertRules
            .Include(a => a.WatchlistItem)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AlertRule>> GetByWatchlistIdAsync(int? watchlistId, CancellationToken cancellationToken)
    {
        var query = _context.AlertRules.Include(a => a.WatchlistItem).AsQueryable();

        if (watchlistId is { } id)
        {
            query = query.Where(a => a.WatchlistItem!.WatchlistId == id);
        }

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertRule>> GetActiveByCurrencyPairsAsync(
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

        var candidates = await _context.AlertRules
            .Include(a => a.WatchlistItem)
            .Where(a => a.IsActive
                && bases.Contains(a.WatchlistItem!.BaseCurrency)
                && quotes.Contains(a.WatchlistItem.QuoteCurrency))
            .ToListAsync(cancellationToken);

        var pairSet = pairs.ToHashSet();
        return candidates
            .Where(a => pairSet.Contains((a.WatchlistItem!.BaseCurrency, a.WatchlistItem.QuoteCurrency)))
            .ToList();
    }

    public void Add(AlertRule alertRule) => _context.AlertRules.Add(alertRule);
}
