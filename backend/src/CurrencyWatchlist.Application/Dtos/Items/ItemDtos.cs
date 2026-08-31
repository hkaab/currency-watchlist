using CurrencyWatchlist.Application.Dtos.Rates;

namespace CurrencyWatchlist.Application.Dtos.Items;

public sealed record CreateWatchlistItemRequest(string BaseCurrency, string QuoteCurrency);

public sealed record WatchlistItemResponse(
    int Id,
    int WatchlistId,
    string BaseCurrency,
    string QuoteCurrency,
    DateTime CreatedAt,
    RateSnapshotResponse? LatestRate);
