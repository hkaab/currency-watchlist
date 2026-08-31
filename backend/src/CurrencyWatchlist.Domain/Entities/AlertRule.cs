using CurrencyWatchlist.Domain.Enums;

namespace CurrencyWatchlist.Domain.Entities;

public class AlertRule
{
    public int Id { get; set; }
    public int WatchlistItemId { get; set; }
    public AlertCondition Condition { get; set; }
    public decimal Threshold { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public WatchlistItem? WatchlistItem { get; set; }

    public bool IsTriggeredBy(decimal rate) => Condition switch
    {
        AlertCondition.Above => rate > Threshold,
        AlertCondition.Below => rate < Threshold,
        _ => throw new ArgumentOutOfRangeException(nameof(Condition), Condition, "Unknown alert condition.")
    };
}
