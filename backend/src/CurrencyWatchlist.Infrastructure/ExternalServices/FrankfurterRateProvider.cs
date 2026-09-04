using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace CurrencyWatchlist.Infrastructure.ExternalServices;

/// <summary>
/// <see cref="IRateProvider"/> implementation backed by the free Frankfurter exchange rate API
/// (https://api.frankfurter.app). Never leaks Frankfurter's wire format past this class.
/// </summary>
public class FrankfurterRateProvider : IRateProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterRateProvider> _logger;

    public FrankfurterRateProvider(HttpClient httpClient, ILogger<FrankfurterRateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RateQuote>> GetLatestRatesAsync(
        string baseCurrency, IReadOnlyCollection<string> quoteCurrencies, CancellationToken cancellationToken)
    {
        var normalizedBase = baseCurrency.Trim().ToUpperInvariant();
        var normalizedQuotes = quoteCurrencies.Select(q => q.Trim().ToUpperInvariant()).Distinct().ToList();
        var requestUri = $"latest?from={Uri.EscapeDataString(normalizedBase)}&to={Uri.EscapeDataString(string.Join(',', normalizedQuotes))}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new RateProviderUnavailableException($"Unable to reach the exchange rate provider: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RateProviderUnavailableException("The exchange rate provider request timed out.", ex);
        }
        catch (BrokenCircuitException ex)
        {
            throw new RateProviderUnavailableException("The exchange rate provider is temporarily unavailable (circuit open).", ex);
        }

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(
                "Rate provider rejected request for {Base} -> {Quotes} with status {StatusCode}",
                normalizedBase, string.Join(",", normalizedQuotes), response.StatusCode);
            throw new UnknownCurrencyException([normalizedBase, .. normalizedQuotes]);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new RateProviderUnavailableException($"Exchange rate provider returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        FrankfurterLatestResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<FrankfurterLatestResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new RateProviderUnavailableException("Exchange rate provider returned an unexpected response format.", ex);
        }

        if (payload?.Rates is null)
        {
            throw new RateProviderUnavailableException("Exchange rate provider returned an empty response.");
        }

        var sourceTimestamp = DateTime.SpecifyKind(payload.Date, DateTimeKind.Utc);
        var results = new List<RateQuote>();
        var missing = new List<string>();

        foreach (var quote in normalizedQuotes)
        {
            if (payload.Rates.TryGetValue(quote, out var rate))
            {
                results.Add(new RateQuote(normalizedBase, quote, rate, sourceTimestamp));
            }
            else
            {
                missing.Add(quote);
            }
        }

        if (results.Count == 0 && missing.Count > 0)
        {
            throw new UnknownCurrencyException(missing[0]);
        }

        if (missing.Count > 0)
        {
            _logger.LogWarning("Rate provider did not return rates for: {Missing}", string.Join(",", missing));
        }

        return results;
    }

    private sealed class FrankfurterLatestResponse
    {
        public decimal Amount { get; set; }

        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
