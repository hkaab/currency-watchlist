using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Domain.Events;
using CurrencyWatchlist.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Api.Tests;

public sealed record TestEvent(string Value) : IDomainEvent;

public class InMemoryEventPublisherTests
{
    [Fact]
    public async Task Invokes_every_registered_handler_for_the_event_type()
    {
        var handler1 = Substitute.For<IDomainEventHandler<TestEvent>>();
        var handler2 = Substitute.For<IDomainEventHandler<TestEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton(handler1);
        services.AddSingleton(handler2);
        var provider = services.BuildServiceProvider();

        var sut = new InMemoryEventPublisher(provider, Substitute.For<ILogger<InMemoryEventPublisher>>());
        var testEvent = new TestEvent("hello");

        await sut.PublishAsync(testEvent, CancellationToken.None);

        await handler1.Received(1).HandleAsync(testEvent, Arg.Any<CancellationToken>());
        await handler2.Received(1).HandleAsync(testEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_failing_handler_does_not_prevent_other_handlers_from_running()
    {
        var failingHandler = Substitute.For<IDomainEventHandler<TestEvent>>();
        failingHandler.HandleAsync(Arg.Any<TestEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));
        var succeedingHandler = Substitute.For<IDomainEventHandler<TestEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton(failingHandler);
        services.AddSingleton(succeedingHandler);
        var provider = services.BuildServiceProvider();

        var sut = new InMemoryEventPublisher(provider, Substitute.For<ILogger<InMemoryEventPublisher>>());

        await sut.PublishAsync(new TestEvent("x"), CancellationToken.None);

        await succeedingHandler.Received(1).HandleAsync(Arg.Any<TestEvent>(), Arg.Any<CancellationToken>());
    }
}
