using CurrencyWatchlist.Domain.Enums;

namespace CurrencyWatchlist.Application.Dtos.Alerts;

public sealed record CreateAlertRuleRequest(int WatchlistItemId, AlertCondition Condition, decimal Threshold);

public sealed record AlertRuleResponse(
    int Id,
    int WatchlistItemId,
    string BaseCurrency,
    string QuoteCurrency,
    AlertCondition Condition,
    decimal Threshold,
    bool IsActive,
    DateTime CreatedAt);

public sealed record AlertEvaluationResult(
    int AlertRuleId,
    bool IsTriggered,
    decimal Rate,
    decimal Threshold,
    AlertCondition Condition,
    DateTime EvaluatedAt,
    int? AlertEventId);
