using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CurrencyWatchlist.Application.EventHandlers;

/// <summary>
/// Reacts to a bulk rate refresh by checking every active alert rule for the refreshed pairs
/// and persisting + publishing an AlertTriggeredEvent for each one that fires.
/// </summary>
public sealed class EvaluateAlertsOnRateRefreshHandler : IDomainEventHandler<RatesRefreshedEvent>
{
    private readonly IAlertRuleRepository _alertRules;
    private readonly IAlertEventRepository _alertEvents;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<EvaluateAlertsOnRateRefreshHandler> _logger;

    public EvaluateAlertsOnRateRefreshHandler(
        IAlertRuleRepository alertRules,
        IAlertEventRepository alertEvents,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<EvaluateAlertsOnRateRefreshHandler> logger)
    {
        _alertRules = alertRules;
        _alertEvents = alertEvents;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(RatesRefreshedEvent domainEvent, CancellationToken cancellationToken)
    {
        var triggered = new List<(AlertEvent Event, int WatchlistId, string Base, string Quote)>();

        var distinctPairs = domainEvent.Snapshots
            .GroupBy(s => (s.BaseCurrency, s.QuoteCurrency))
            .Select(g => g.Last());

        foreach (var snapshot in distinctPairs)
        {
            var rules = await _alertRules.GetActiveByCurrencyPairAsync(snapshot.BaseCurrency, snapshot.QuoteCurrency, cancellationToken);

            foreach (var rule in rules)
            {
                if (!rule.IsTriggeredBy(snapshot.Rate))
                {
                    continue;
                }

                var alertEvent = new AlertEvent
                {
                    AlertRuleId = rule.Id,
                    TriggeredAt = snapshot.FetchedAt,
                    Rate = snapshot.Rate,
                    Message = $"{snapshot.BaseCurrency}->{snapshot.QuoteCurrency} is {rule.Condition} threshold {rule.Threshold} (current rate: {snapshot.Rate})"
                };
                _alertEvents.Add(alertEvent);
                triggered.Add((alertEvent, rule.WatchlistItem!.WatchlistId, snapshot.BaseCurrency, snapshot.QuoteCurrency));
            }
        }

        if (triggered.Count == 0)
        {
            return;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{Count} alert(s) triggered by rate refresh", triggered.Count);

        foreach (var (alertEvent, watchlistId, baseCurrency, quoteCurrency) in triggered)
        {
            await _eventPublisher.PublishAsync(
                new AlertTriggeredEvent(alertEvent, watchlistId, baseCurrency, quoteCurrency),
                cancellationToken);
        }
    }
}
