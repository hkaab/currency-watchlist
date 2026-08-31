using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IRateSnapshotRepository
{
    Task<RateSnapshot?> GetLatestAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);

    Task<IReadOnlyList<RateSnapshot>> GetHistoryAsync(
        string baseCurrency,
        string quoteCurrency,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    void Add(RateSnapshot snapshot);
    void AddRange(IEnumerable<RateSnapshot> snapshots);
}
