namespace CurrencyWatchlist.Application.Common.Exceptions;

/// <summary>Raised when adding a watchlist item that duplicates an existing base/quote pair on the same watchlist.</summary>
public sealed class DuplicateWatchlistItemException : Exception
{
    public DuplicateWatchlistItemException(string baseCurrency, string quoteCurrency)
        : base($"This watchlist already tracks {baseCurrency}/{quoteCurrency}.")
    {
    }
}
