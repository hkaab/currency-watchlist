using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Events;

/// <summary>
/// Pushes live updates to connected frontend clients for a given watchlist.
/// Implemented at the web layer (SignalR) so the Application layer stays transport-agnostic.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyRatesUpdatedAsync(int watchlistId, IReadOnlyList<RateSnapshot> snapshots, CancellationToken cancellationToken);

    Task NotifyAlertTriggeredAsync(int watchlistId, AlertEvent alertEvent, CancellationToken cancellationToken);
}
