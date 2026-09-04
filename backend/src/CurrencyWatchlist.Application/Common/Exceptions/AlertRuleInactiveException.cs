namespace CurrencyWatchlist.Application.Common.Exceptions;

/// <summary>Raised when an action requires an <see cref="Domain.Entities.AlertRule"/> to be active but it is not.</summary>
public sealed class AlertRuleInactiveException : Exception
{
    public AlertRuleInactiveException(int alertRuleId)
        : base($"Alert rule {alertRuleId} is inactive and cannot be evaluated.")
    {
    }
}
