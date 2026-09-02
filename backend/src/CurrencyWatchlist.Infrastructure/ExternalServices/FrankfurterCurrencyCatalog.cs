using System.Net.Http.Json;
using CurrencyWatchlist.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CurrencyWatchlist.Infrastructure.ExternalServices;

/// <summary>
/// Validates currency codes against Frankfurter's own supported set (its <c>/currencies</c>
/// endpoint), not a static ISO 4217 list - Frankfurter (backed by the ECB) only supports a
/// few dozen major currencies, not all ~180 ISO codes, so ISO validity alone isn't enough to
/// guarantee a code will actually work with this app's one rate source.
///
/// The list changes essentially never, so it's cached for the process lifetime once fetched
/// successfully. If Frankfurter can't be reached, validation fails *open* (treats the code as
/// supported) rather than blocking every watchlist mutation on a third-party outage - the
/// existing rate-refresh error handling (400/502) is still the backstop if the code turns out
/// to be genuinely invalid once rates are actually fetched.
/// </summary>
public class FrankfurterCurrencyCatalog : ICurrencyCatalog
{
    private const string CacheKey = "frankfurter-supported-currencies";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FrankfurterCurrencyCatalog> _logger;

    public FrankfurterCurrencyCatalog(HttpClient httpClient, IMemoryCache cache, ILogger<FrankfurterCurrencyCatalog> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsSupportedAsync(string currencyCode, CancellationToken cancellationToken)
    {
        var supported = await GetSupportedCodesAsync(cancellationToken);
        return supported is null || supported.Contains(currencyCode.Trim().ToUpperInvariant());
    }

    private Task<HashSet<string>?> GetSupportedCodesAsync(CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync<HashSet<string>?>(CacheKey, async entry =>
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>("currencies", cancellationToken);
                if (response is null || response.Count == 0)
                {
                    throw new InvalidOperationException("Frankfurter returned an empty currency list.");
                }

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return response.Keys.Select(k => k.ToUpperInvariant()).ToHashSet();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch the supported currency list from Frankfurter; currency validation will fail open until the next attempt");
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1); // retry soon, don't cache the failure for long
                return null;
            }
        });
}
