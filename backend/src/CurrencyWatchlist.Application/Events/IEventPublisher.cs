using CurrencyWatchlist.Domain.Events;

namespace CurrencyWatchlist.Application.Events;

/// <summary>
/// Publishes a domain event to every registered <see cref="IDomainEventHandler{TEvent}"/> for its type.
/// New handlers plug in via DI registration alone (Open/Closed) - publishers never change.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken) where TEvent : IDomainEvent;
}
