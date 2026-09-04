using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Watchlists;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Mappings;
using CurrencyWatchlist.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CurrencyWatchlist.Application.Services;

public sealed class WatchlistService : IWatchlistService
{
    private readonly IWatchlistRepository _watchlists;
    private readonly IRateSnapshotRepository _rateSnapshots;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WatchlistService> _logger;

    public WatchlistService(
        IWatchlistRepository watchlists,
        IRateSnapshotRepository rateSnapshots,
        IUnitOfWork unitOfWork,
        ILogger<WatchlistService> logger)
    {
        _watchlists = watchlists;
        _rateSnapshots = rateSnapshots;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WatchlistResponse> CreateAsync(CreateWatchlistRequest request, CancellationToken cancellationToken)
    {
        var watchlist = new Watchlist
        {
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _watchlists.Add(watchlist);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created watchlist {WatchlistId} ({Name})", watchlist.Id, watchlist.Name);
        return watchlist.ToResponse();
    }

    public async Task<IReadOnlyList<WatchlistResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var watchlists = await _watchlists.GetAllAsync(cancellationToken);
        return watchlists.Select(w => w.ToResponse()).ToList();
    }

    public async Task<WatchlistDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var watchlist = await _watchlists.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Watchlist), id);

        var pairs = watchlist.Items.Select(i => (i.BaseCurrency, i.QuoteCurrency)).Distinct().ToList();
        var snapshots = await _rateSnapshots.GetLatestForPairsAsync(pairs, cancellationToken);
        var snapshotsByPair = snapshots.ToDictionary(s => (s.BaseCurrency, s.QuoteCurrency));

        var latestRates = watchlist.Items.ToDictionary(
            i => i.Id,
            i => snapshotsByPair.GetValueOrDefault((i.BaseCurrency, i.QuoteCurrency)));

        return watchlist.ToDetailResponse(latestRates);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var watchlist = await _watchlists.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Watchlist), id);

        _watchlists.Remove(watchlist);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted watchlist {WatchlistId}", id);
    }
}
