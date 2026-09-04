using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Alerts;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Mappings;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CurrencyWatchlist.Application.Services;

public sealed class AlertService : IAlertService
{
    private readonly IWatchlistItemRepository _items;
    private readonly IAlertRuleRepository _alertRules;
    private readonly IAlertEventRepository _alertEvents;
    private readonly IRateSnapshotRepository _rateSnapshots;
    private readonly IRateProvider _rateProvider;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IWatchlistItemRepository items,
        IAlertRuleRepository alertRules,
        IAlertEventRepository alertEvents,
        IRateSnapshotRepository rateSnapshots,
        IRateProvider rateProvider,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<AlertService> logger)
    {
        _items = items;
        _alertRules = alertRules;
        _alertEvents = alertEvents;
        _rateSnapshots = rateSnapshots;
        _rateProvider = rateProvider;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AlertRuleResponse> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.WatchlistItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(WatchlistItem), request.WatchlistItemId);

        var rule = new AlertRule
        {
            WatchlistItemId = item.Id,
            Condition = request.Condition,
            Threshold = request.Threshold,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            WatchlistItem = item
        };

        _alertRules.Add(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created alert rule {AlertRuleId} for item {ItemId} ({Base}/{Quote}) {Condition} {Threshold}",
            rule.Id, item.Id, item.BaseCurrency, item.QuoteCurrency, rule.Condition, rule.Threshold);

        return rule.ToResponse();
    }

    public async Task<IReadOnlyList<AlertRuleResponse>> GetAllAsync(int? watchlistId, CancellationToken cancellationToken)
    {
        var rules = await _alertRules.GetByWatchlistIdAsync(watchlistId, cancellationToken);
        return rules.Select(r => r.ToResponse()).ToList();
    }

    public async Task<AlertEvaluationResult> EvaluateAsync(int alertRuleId, CancellationToken cancellationToken)
    {
        var rule = await _alertRules.GetByIdWithItemAsync(alertRuleId, cancellationToken)
            ?? throw new NotFoundException(nameof(AlertRule), alertRuleId);

        if (!rule.IsActive)
        {
            throw new AlertRuleInactiveException(rule.Id);
        }

        var item = rule.WatchlistItem!;
        var quotes = await _rateProvider.GetLatestRatesAsync(item.BaseCurrency, [item.QuoteCurrency], cancellationToken);
        var quote = quotes.FirstOrDefault()
            ?? throw new RateProviderUnavailableException($"No rate was returned for {item.BaseCurrency}/{item.QuoteCurrency}.");
        var evaluatedAt = DateTime.UtcNow;

        _rateSnapshots.Add(new RateSnapshot
        {
            BaseCurrency = quote.BaseCurrency,
            QuoteCurrency = quote.QuoteCurrency,
            Rate = quote.Rate,
            SourceTimestamp = quote.SourceTimestamp,
            FetchedAt = evaluatedAt
        });

        var isTriggered = rule.IsTriggeredBy(quote.Rate);
        AlertEvent? alertEvent = null;

        if (isTriggered)
        {
            alertEvent = new AlertEvent
            {
                AlertRuleId = rule.Id,
                TriggeredAt = evaluatedAt,
                Rate = quote.Rate,
                Message = $"{item.BaseCurrency}->{item.QuoteCurrency} is {rule.Condition} threshold {rule.Threshold} (current rate: {quote.Rate})"
            };
            _alertEvents.Add(alertEvent);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (isTriggered)
        {
            _logger.LogInformation("Alert rule {AlertRuleId} triggered at rate {Rate}", rule.Id, quote.Rate);
            await _eventPublisher.PublishAsync(
                new AlertTriggeredEvent(alertEvent!, item.WatchlistId, item.BaseCurrency, item.QuoteCurrency),
                cancellationToken);
        }

        return new AlertEvaluationResult(rule.Id, isTriggered, quote.Rate, rule.Threshold, rule.Condition, evaluatedAt, alertEvent?.Id);
    }
}
