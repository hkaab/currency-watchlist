using CurrencyWatchlist.Application.Dtos.Rates;

namespace CurrencyWatchlist.Application.Services;

public interface IRateService
{
    /// <param name="watchlistId">When provided, only pairs belonging to this watchlist are refreshed; otherwise every distinct pair across all watchlists is refreshed.</param>
    Task<RefreshRatesResponse> RefreshAsync(int? watchlistId, CancellationToken cancellationToken);

    Task<RateSnapshotResponse> GetLatestAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);

    Task<IReadOnlyList<RateSnapshotResponse>> GetHistoryAsync(
        string baseCurrency,
        string quoteCurrency,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
