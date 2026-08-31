using CurrencyWatchlist.Application.Dtos.Items;

namespace CurrencyWatchlist.Application.Dtos.Watchlists;

public sealed record CreateWatchlistRequest(string Name);

public sealed record WatchlistResponse(int Id, string Name, DateTime CreatedAt, int ItemCount);

public sealed record WatchlistDetailResponse(int Id, string Name, DateTime CreatedAt, IReadOnlyList<WatchlistItemResponse> Items);
