using CurrencyWatchlist.Domain.Events;

namespace CurrencyWatchlist.Application.Events;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
