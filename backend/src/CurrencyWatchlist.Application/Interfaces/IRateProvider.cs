namespace CurrencyWatchlist.Application.Interfaces;

/// <summary>
/// Abstraction over an external exchange-rate source. Swapping providers (e.g. away from Frankfurter)
/// only requires a new implementation of this interface - no consumer code changes (Open/Closed).
/// </summary>
public interface IRateProvider
{
    /// <summary>
    /// Fetches the latest rate for <paramref name="baseCurrency"/> against each of <paramref name="quoteCurrencies"/>
    /// in a single call where the provider supports it.
    /// </summary>
    /// <exception cref="Common.Exceptions.UnknownCurrencyException">A requested currency code is not recognized.</exception>
    /// <exception cref="Common.Exceptions.RateProviderUnavailableException">The provider could not be reached or failed.</exception>
    Task<IReadOnlyList<RateQuote>> GetLatestRatesAsync(
        string baseCurrency,
        IReadOnlyCollection<string> quoteCurrencies,
        CancellationToken cancellationToken);
}
