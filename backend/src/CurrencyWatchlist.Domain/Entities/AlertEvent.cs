namespace CurrencyWatchlist.Domain.Entities;

public class AlertEvent
{
    public int Id { get; set; }
    public int AlertRuleId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public decimal Rate { get; set; }
    public string Message { get; set; } = string.Empty;

    public AlertRule? AlertRule { get; set; }
}
