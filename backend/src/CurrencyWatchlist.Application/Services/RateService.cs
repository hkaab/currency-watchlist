using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Rates;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Mappings;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CurrencyWatchlist.Application.Services;

public sealed class RateService : IRateService
{
    private readonly IWatchlistRepository _watchlists;
    private readonly IWatchlistItemRepository _items;
    private readonly IRateSnapshotRepository _rateSnapshots;
    private readonly IRateProvider _rateProvider;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RateService> _logger;

    public RateService(
        IWatchlistRepository watchlists,
        IWatchlistItemRepository items,
        IRateSnapshotRepository rateSnapshots,
        IRateProvider rateProvider,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<RateService> logger)
    {
        _watchlists = watchlists;
        _items = items;
        _rateSnapshots = rateSnapshots;
        _rateProvider = rateProvider;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RefreshRatesResponse> RefreshAsync(int? watchlistId, CancellationToken cancellationToken)
    {
        IReadOnlyList<WatchlistItem> items;
        if (watchlistId is { } id)
        {
            _ = await _watchlists.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Watchlist), id);
            items = await _items.GetByWatchlistIdAsync(id, cancellationToken);
        }
        else
        {
            items = await _items.GetAllAsync(cancellationToken);
        }

        var pairsByBase = items
            .GroupBy(i => i.BaseCurrency)
            .ToDictionary(g => g.Key, g => g.Select(i => i.QuoteCurrency).Distinct().ToList());

        var snapshots = new List<RateSnapshot>();
        RateProviderException? lastFailure = null;

        foreach (var (baseCurrency, quoteCurrencies) in pairsByBase)
        {
            try
            {
                var quotes = await _rateProvider.GetLatestRatesAsync(baseCurrency, quoteCurrencies, cancellationToken);
                var fetchedAt = DateTime.UtcNow;
                snapshots.AddRange(quotes.Select(q => new RateSnapshot
                {
                    BaseCurrency = q.BaseCurrency,
                    QuoteCurrency = q.QuoteCurrency,
                    Rate = q.Rate,
                    SourceTimestamp = q.SourceTimestamp,
                    FetchedAt = fetchedAt
                }));
            }
            catch (RateProviderException ex)
            {
                lastFailure = ex;
                _logger.LogWarning(ex, "Failed to refresh rates for base currency {BaseCurrency}", baseCurrency);
            }
        }

        if (snapshots.Count == 0 && lastFailure is not null)
        {
            throw lastFailure;
        }

        if (snapshots.Count > 0)
        {
            _rateSnapshots.AddRange(snapshots);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refreshed {Count} rate snapshot(s)", snapshots.Count);

            await _eventPublisher.PublishAsync(new RatesRefreshedEvent(snapshots), cancellationToken);
        }

        return new RefreshRatesResponse(snapshots.Count, snapshots.Select(s => s.ToResponse()).ToList());
    }

    public async Task<RateSnapshotResponse> GetLatestAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken)
    {
        var (b, q) = Normalize(baseCurrency, quoteCurrency);
        var snapshot = await _rateSnapshots.GetLatestAsync(b, q, cancellationToken)
            ?? throw new NotFoundException("RateSnapshot", $"{b}/{q}");

        return snapshot.ToResponse();
    }

    public async Task<IReadOnlyList<RateSnapshotResponse>> GetHistoryAsync(
        string baseCurrency, string quoteCurrency, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var (b, q) = Normalize(baseCurrency, quoteCurrency);
        var history = await _rateSnapshots.GetHistoryAsync(b, q, from, to, cancellationToken);
        return history.Select(s => s.ToResponse()).ToList();
    }

    private static (string Base, string Quote) Normalize(string baseCurrency, string quoteCurrency) =>
        (baseCurrency.Trim().ToUpperInvariant(), quoteCurrency.Trim().ToUpperInvariant());
}
