using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Domain.Events;

/// <summary>Published after one or more rate snapshots have been fetched and persisted.</summary>
public sealed class RatesRefreshedEvent : IDomainEvent
{
    public RatesRefreshedEvent(IReadOnlyList<RateSnapshot> snapshots)
    {
        Snapshots = snapshots;
    }

    public IReadOnlyList<RateSnapshot> Snapshots { get; }
}
