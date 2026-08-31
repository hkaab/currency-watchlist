using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CurrencyWatchlist.Infrastructure.Events;

/// <summary>
/// Resolves every <see cref="IDomainEventHandler{TEvent}"/> registered for the published event's type
/// from the current DI scope and invokes them. No external broker - see README for the production tradeoff.
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(IServiceProvider serviceProvider, ILogger<InMemoryEventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken) where TEvent : IDomainEvent
    {
        var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>().ToList();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed while processing {Event}", handler.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}
