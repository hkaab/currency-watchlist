using CurrencyWatchlist.Application.Dtos.Alerts;

namespace CurrencyWatchlist.Application.Services;

public interface IAlertService
{
    Task<AlertRuleResponse> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRuleResponse>> GetAllAsync(int? watchlistId, CancellationToken cancellationToken);

    /// <summary>Fetches the latest rate, checks this rule's condition only, and persists+publishes an AlertEvent on trigger.</summary>
    Task<AlertEvaluationResult> EvaluateAsync(int alertRuleId, CancellationToken cancellationToken);
}
