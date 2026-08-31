namespace CurrencyWatchlist.Domain.Entities;

public class RateSnapshot
{
    public int Id { get; set; }
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }

    /// <summary>The date the rate was quoted for, as reported by the external provider.</summary>
    public DateTime SourceTimestamp { get; set; }

    /// <summary>When this snapshot was fetched and stored by this system.</summary>
    public DateTime FetchedAt { get; set; }
}
