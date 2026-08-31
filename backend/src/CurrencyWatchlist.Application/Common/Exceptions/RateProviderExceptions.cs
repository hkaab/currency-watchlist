namespace CurrencyWatchlist.Application.Common.Exceptions;

/// <summary>Base type for failures raised by an <see cref="Interfaces.IRateProvider"/> implementation.</summary>
public abstract class RateProviderException : Exception
{
    protected RateProviderException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>The external provider does not recognize one of the requested currency codes.</summary>
public sealed class UnknownCurrencyException : RateProviderException
{
    public UnknownCurrencyException(string currencyCode)
        : base($"Currency code '{currencyCode}' is not recognized by the rate provider.")
    {
    }

    public UnknownCurrencyException(IEnumerable<string> candidateCurrencyCodes)
        : base($"One or more of these currency codes are not recognized by the rate provider: {string.Join(", ", candidateCurrencyCodes)}.")
    {
    }
}

/// <summary>The external provider could not be reached or returned a server error.</summary>
public sealed class RateProviderUnavailableException : RateProviderException
{
    public RateProviderUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
