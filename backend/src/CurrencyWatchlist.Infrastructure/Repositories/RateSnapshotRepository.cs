using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CurrencyWatchlist.Infrastructure.Repositories;

public class RateSnapshotRepository : IRateSnapshotRepository
{
    private readonly CurrencyWatchlistDbContext _context;

    public RateSnapshotRepository(CurrencyWatchlistDbContext context) => _context = context;

    public Task<RateSnapshot?> GetLatestAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken) =>
        _context.RateSnapshots
            .Where(s => s.BaseCurrency == baseCurrency && s.QuoteCurrency == quoteCurrency)
            .OrderByDescending(s => s.SourceTimestamp)
            .ThenByDescending(s => s.FetchedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<RateSnapshot>> GetLatestForPairsAsync(
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

        var candidates = await _context.RateSnapshots
            .Where(s => bases.Contains(s.BaseCurrency) && quotes.Contains(s.QuoteCurrency))
            .ToListAsync(cancellationToken);

        var pairSet = pairs.ToHashSet();

        return candidates
            .Where(s => pairSet.Contains((s.BaseCurrency, s.QuoteCurrency)))
            .GroupBy(s => (s.BaseCurrency, s.QuoteCurrency))
            .Select(g => g.OrderByDescending(s => s.SourceTimestamp).ThenByDescending(s => s.FetchedAt).First())
            .ToList();
    }

    public async Task<IReadOnlyList<RateSnapshot>> GetHistoryAsync(
        string baseCurrency, string quoteCurrency, DateTime from, DateTime to, CancellationToken cancellationToken) =>
        await _context.RateSnapshots
            .Where(s => s.BaseCurrency == baseCurrency
                && s.QuoteCurrency == quoteCurrency
                && s.SourceTimestamp >= from
                && s.SourceTimestamp <= to)
            .OrderBy(s => s.SourceTimestamp)
            .ToListAsync(cancellationToken);

    public void Add(RateSnapshot snapshot) => _context.RateSnapshots.Add(snapshot);

    public void AddRange(IEnumerable<RateSnapshot> snapshots) => _context.RateSnapshots.AddRange(snapshots);
}
