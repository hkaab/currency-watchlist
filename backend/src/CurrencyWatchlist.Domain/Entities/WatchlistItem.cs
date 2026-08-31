namespace CurrencyWatchlist.Domain.Entities;

public class WatchlistItem
{
    public int Id { get; set; }
    public int WatchlistId { get; set; }
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Watchlist? Watchlist { get; set; }
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
}
