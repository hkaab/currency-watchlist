using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Alerts;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Services;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Enums;
using CurrencyWatchlist.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.Services;

public class AlertServiceTests
{
    private readonly IWatchlistItemRepository _items = Substitute.For<IWatchlistItemRepository>();
    private readonly IAlertRuleRepository _alertRules = Substitute.For<IAlertRuleRepository>();
    private readonly IAlertEventRepository _alertEvents = Substitute.For<IAlertEventRepository>();
    private readonly IRateSnapshotRepository _rateSnapshots = Substitute.For<IRateSnapshotRepository>();
    private readonly IRateProvider _rateProvider = Substitute.For<IRateProvider>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AlertService _sut;

    public AlertServiceTests()
    {
        _sut = new AlertService(
            _items, _alertRules, _alertEvents, _rateSnapshots, _rateProvider, _eventPublisher, _unitOfWork,
            Substitute.For<ILogger<AlertService>>());
    }

    [Fact]
    public async Task CreateAsync_throws_NotFound_when_item_missing()
    {
        _items.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((WatchlistItem?)null);

        var act = () => _sut.CreateAsync(new CreateAlertRuleRequest(1, AlertCondition.Above, 1.6m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_persists_rule_and_returns_response()
    {
        var item = new WatchlistItem { Id = 1, BaseCurrency = "USD", QuoteCurrency = "AUD" };
        _items.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.CreateAsync(new CreateAlertRuleRequest(1, AlertCondition.Above, 1.6m), CancellationToken.None);

        result.Condition.Should().Be(AlertCondition.Above);
        result.Threshold.Should().Be(1.6m);
        result.BaseCurrency.Should().Be("USD");
        _alertRules.Received(1).Add(Arg.Any<AlertRule>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_throws_NotFound_when_rule_missing()
    {
        _alertRules.GetByIdWithItemAsync(1, Arg.Any<CancellationToken>()).Returns((AlertRule?)null);

        var act = () => _sut.EvaluateAsync(1, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task EvaluateAsync_throws_when_rule_is_inactive()
    {
        var item = new WatchlistItem { Id = 1, WatchlistId = 5, BaseCurrency = "USD", QuoteCurrency = "AUD" };
        var rule = new AlertRule { Id = 1, Condition = AlertCondition.Above, Threshold = 1.0m, WatchlistItem = item, IsActive = false };
        _alertRules.GetByIdWithItemAsync(1, Arg.Any<CancellationToken>()).Returns(rule);

        var act = () => _sut.EvaluateAsync(1, CancellationToken.None);

        await act.Should().ThrowAsync<AlertRuleInactiveException>();
        await _rateProvider.DidNotReceive().GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_throws_when_provider_returns_no_quotes()
    {
        var item = new WatchlistItem { Id = 1, WatchlistId = 5, BaseCurrency = "USD", QuoteCurrency = "AUD" };
        var rule = new AlertRule { Id = 1, Condition = AlertCondition.Above, Threshold = 1.0m, WatchlistItem = item };
        _alertRules.GetByIdWithItemAsync(1, Arg.Any<CancellationToken>()).Returns(rule);
        _rateProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote>());

        var act = () => _sut.EvaluateAsync(1, CancellationToken.None);

        await act.Should().ThrowAsync<RateProviderUnavailableException>();
    }

    [Fact]
    public async Task EvaluateAsync_persists_snapshot_but_not_alert_event_when_not_triggered()
    {
        var item = new WatchlistItem { Id = 1, WatchlistId = 5, BaseCurrency = "USD", QuoteCurrency = "AUD" };
        var rule = new AlertRule { Id = 1, Condition = AlertCondition.Above, Threshold = 2.0m, WatchlistItem = item };
        _alertRules.GetByIdWithItemAsync(1, Arg.Any<CancellationToken>()).Returns(rule);
        _rateProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote> { new("USD", "AUD", 1.5m, DateTime.UtcNow) });

        var result = await _sut.EvaluateAsync(1, CancellationToken.None);

        result.IsTriggered.Should().BeFalse();
        result.AlertEventId.Should().BeNull();
        _rateSnapshots.Received(1).Add(Arg.Any<RateSnapshot>());
        _alertEvents.DidNotReceive().Add(Arg.Any<AlertEvent>());
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<AlertTriggeredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_persists_and_publishes_when_triggered()
    {
        var item = new WatchlistItem { Id = 1, WatchlistId = 5, BaseCurrency = "USD", QuoteCurrency = "AUD" };
        var rule = new AlertRule { Id = 1, Condition = AlertCondition.Above, Threshold = 1.0m, WatchlistItem = item };
        _alertRules.GetByIdWithItemAsync(1, Arg.Any<CancellationToken>()).Returns(rule);
        _rateProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote> { new("USD", "AUD", 1.5m, DateTime.UtcNow) });

        var result = await _sut.EvaluateAsync(1, CancellationToken.None);

        result.IsTriggered.Should().BeTrue();
        result.AlertEventId.Should().NotBeNull();
        _alertEvents.Received(1).Add(Arg.Any<AlertEvent>());
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<AlertTriggeredEvent>(e => e.WatchlistId == 5 && e.BaseCurrency == "USD" && e.QuoteCurrency == "AUD"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_delegates_to_repository()
    {
        _alertRules.GetByWatchlistIdAsync(5, Arg.Any<CancellationToken>()).Returns(new List<AlertRule>
        {
            new() { Id = 1, WatchlistItem = new WatchlistItem { BaseCurrency = "USD", QuoteCurrency = "AUD" } }
        });

        var result = await _sut.GetAllAsync(5, CancellationToken.None);

        result.Should().ContainSingle();
    }
}
