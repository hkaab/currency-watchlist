using CurrencyWatchlist.Application.Dtos.Alerts;
using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Dtos.Rates;
using CurrencyWatchlist.Application.Dtos.Watchlists;
using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Mappings;

public static class MappingExtensions
{
    public static WatchlistResponse ToResponse(this Watchlist watchlist) =>
        new(watchlist.Id, watchlist.Name, watchlist.CreatedAt, watchlist.Items.Count);

    public static WatchlistDetailResponse ToDetailResponse(this Watchlist watchlist, IReadOnlyDictionary<int, RateSnapshot?> latestRates) =>
        new(
            watchlist.Id,
            watchlist.Name,
            watchlist.CreatedAt,
            watchlist.Items.Select(i => i.ToResponse(latestRates.GetValueOrDefault(i.Id))).ToList());

    public static WatchlistItemResponse ToResponse(this WatchlistItem item, RateSnapshot? latestRate) =>
        new(item.Id, item.WatchlistId, item.BaseCurrency, item.QuoteCurrency, item.CreatedAt, latestRate?.ToResponse());

    public static RateSnapshotResponse ToResponse(this RateSnapshot snapshot) =>
        new(snapshot.Id, snapshot.BaseCurrency, snapshot.QuoteCurrency, snapshot.Rate, snapshot.SourceTimestamp, snapshot.FetchedAt);

    public static AlertRuleResponse ToResponse(this AlertRule rule) =>
        new(
            rule.Id,
            rule.WatchlistItemId,
            rule.WatchlistItem?.BaseCurrency ?? string.Empty,
            rule.WatchlistItem?.QuoteCurrency ?? string.Empty,
            rule.Condition,
            rule.Threshold,
            rule.IsActive,
            rule.CreatedAt);
}
