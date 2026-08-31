namespace CurrencyWatchlist.Application.Dtos.Rates;

public sealed record RateSnapshotResponse(
    int Id,
    string BaseCurrency,
    string QuoteCurrency,
    decimal Rate,
    DateTime SourceTimestamp,
    DateTime FetchedAt);

public sealed record RefreshRatesResponse(int RefreshedPairCount, IReadOnlyList<RateSnapshotResponse> Snapshots);
