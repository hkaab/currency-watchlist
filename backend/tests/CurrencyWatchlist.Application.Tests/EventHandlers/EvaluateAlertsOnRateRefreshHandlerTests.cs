using CurrencyWatchlist.Application.EventHandlers;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Enums;
using CurrencyWatchlist.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.EventHandlers;

public class EvaluateAlertsOnRateRefreshHandlerTests
{
    private readonly IAlertRuleRepository _alertRules = Substitute.For<IAlertRuleRepository>();
    private readonly IAlertEventRepository _alertEvents = Substitute.For<IAlertEventRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly EvaluateAlertsOnRateRefreshHandler _sut;

    public EvaluateAlertsOnRateRefreshHandlerTests()
    {
        _sut = new EvaluateAlertsOnRateRefreshHandler(
            _alertRules, _alertEvents, _unitOfWork, _eventPublisher,
            Substitute.For<ILogger<EvaluateAlertsOnRateRefreshHandler>>());
    }

    [Fact]
    public async Task Triggered_rule_persists_event_and_publishes()
    {
        var item = new WatchlistItem { Id = 1, WatchlistId = 7, BaseCurrency = "USD", QuoteCurrency = "AUD" };
        var rule = new AlertRule { Id = 1, Condition = AlertCondition.Above, Threshold = 1.0m, IsActive = true, WatchlistItem = item };
        _alertRules.GetActiveByCurrencyPairAsync("USD", "AUD", Arg.Any<CancellationToken>()).Returns(new List<AlertRule> { rule });

        var snapshot = new RateSnapshot { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m, FetchedAt = DateTime.UtcNow };
        await _sut.HandleAsync(new RatesRefreshedEvent([snapshot]), CancellationToken.None);

        _alertEvents.Received(1).Add(Arg.Any<AlertEvent>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<AlertTriggeredEvent>(e => e.WatchlistId == 7), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_triggered_rule_does_nothing()
    {
        var item = new WatchlistItem { Id = 1, WatchlistId = 7, BaseCurrency = "USD", QuoteCurrency = "AUD" };
        var rule = new AlertRule { Id = 1, Condition = AlertCondition.Above, Threshold = 2.0m, IsActive = true, WatchlistItem = item };
        _alertRules.GetActiveByCurrencyPairAsync("USD", "AUD", Arg.Any<CancellationToken>()).Returns(new List<AlertRule> { rule });

        var snapshot = new RateSnapshot { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m, FetchedAt = DateTime.UtcNow };
        await _sut.HandleAsync(new RatesRefreshedEvent([snapshot]), CancellationToken.None);

        _alertEvents.DidNotReceive().Add(Arg.Any<AlertEvent>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<AlertTriggeredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deduplicates_multiple_snapshots_for_the_same_pair()
    {
        _alertRules.GetActiveByCurrencyPairAsync("USD", "AUD", Arg.Any<CancellationToken>()).Returns(new List<AlertRule>());

        var snapshots = new List<RateSnapshot>
        {
            new() { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.5m },
            new() { BaseCurrency = "USD", QuoteCurrency = "AUD", Rate = 1.6m }
        };
        await _sut.HandleAsync(new RatesRefreshedEvent(snapshots), CancellationToken.None);

        await _alertRules.Received(1).GetActiveByCurrencyPairAsync("USD", "AUD", Arg.Any<CancellationToken>());
    }
}
