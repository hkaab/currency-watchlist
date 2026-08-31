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

    public async Task<IReadOnlyList<AlertRule>> GetActiveByCurrencyPairAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken) =>
        await _context.AlertRules
            .Include(a => a.WatchlistItem)
            .Where(a => a.IsActive
                && a.WatchlistItem!.BaseCurrency == baseCurrency
                && a.WatchlistItem.QuoteCurrency == quoteCurrency)
            .ToListAsync(cancellationToken);

    public void Add(AlertRule alertRule) => _context.AlertRules.Add(alertRule);
}
