namespace CurrencyWatchlist.Application.Interfaces;

/// <summary>
/// Knows which currency codes the rate provider actually supports - distinct from
/// <see cref="IRateProvider"/>, which only knows how to fetch rates (ISP).
/// </summary>
public interface ICurrencyCatalog
{
    Task<bool> IsSupportedAsync(string currencyCode, CancellationToken cancellationToken);
}
