using CurrencyWatchlist.Application.EventHandlers;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Events;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.EventHandlers;

public class PushAlertNotificationHandlerTests
{
    [Fact]
    public async Task Forwards_the_alert_event_to_the_notifier()
    {
        var notifier = Substitute.For<IRealtimeNotifier>();
        var sut = new PushAlertNotificationHandler(notifier);
        var alertEvent = new AlertEvent { Id = 1, AlertRuleId = 2, Rate = 1.6m, Message = "triggered" };

        await sut.HandleAsync(new AlertTriggeredEvent(alertEvent, 9, "USD", "AUD"), CancellationToken.None);

        await notifier.Received(1).NotifyAlertTriggeredAsync(9, alertEvent, Arg.Any<CancellationToken>());
    }
}
