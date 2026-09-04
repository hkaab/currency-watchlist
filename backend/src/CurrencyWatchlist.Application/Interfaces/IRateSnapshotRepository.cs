using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IRateSnapshotRepository
{
    Task<RateSnapshot?> GetLatestAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);

    /// <summary>Latest snapshot per pair, fetched in one round trip instead of one query per pair.</summary>
    Task<IReadOnlyList<RateSnapshot>> GetLatestForPairsAsync(
        IReadOnlyCollection<(string BaseCurrency, string QuoteCurrency)> pairs, CancellationToken cancellationToken);

    Task<IReadOnlyList<RateSnapshot>> GetHistoryAsync(
        string baseCurrency,
        string quoteCurrency,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    void Add(RateSnapshot snapshot);
    void AddRange(IEnumerable<RateSnapshot> snapshots);
}
