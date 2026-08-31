using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IAlertRuleRepository
{
    Task<AlertRule?> GetByIdWithItemAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRule>> GetByWatchlistIdAsync(int? watchlistId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRule>> GetActiveByCurrencyPairAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);
    void Add(AlertRule alertRule);
}
