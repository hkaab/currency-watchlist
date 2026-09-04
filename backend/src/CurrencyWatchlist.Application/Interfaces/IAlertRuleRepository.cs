using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IAlertRuleRepository
{
    Task<AlertRule?> GetByIdWithItemAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRule>> GetByWatchlistIdAsync(int? watchlistId, CancellationToken cancellationToken);
    /// <summary>Active rules across all watchlists matching any of the given pairs, in one round trip.</summary>
    Task<IReadOnlyList<AlertRule>> GetActiveByCurrencyPairsAsync(
        IReadOnlyCollection<(string BaseCurrency, string QuoteCurrency)> pairs, CancellationToken cancellationToken);
    void Add(AlertRule alertRule);
}
