using CurrencyWatchlist.Application.Dtos.Watchlists;

namespace CurrencyWatchlist.Application.Services;

public interface IWatchlistService
{
    Task<WatchlistResponse> CreateAsync(CreateWatchlistRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WatchlistResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<WatchlistDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
