namespace CurrencyWatchlist.Application.Interfaces;

/// <summary>A single base->quote rate as reported by an external provider, decoupled from that provider's wire format.</summary>
public sealed record RateQuote(string BaseCurrency, string QuoteCurrency, decimal Rate, DateTime SourceTimestamp);
