using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace CurrencyWatchlist.Infrastructure.ExternalServices;

/// <summary>
/// Retry + circuit breaker for the rate provider's HttpClient. Kept as a standalone,
/// reusable pipeline (rather than inline in DependencyInjection.cs) so tests can build the
/// exact same resilience behavior against a stubbed transport.
/// </summary>
public static class RateProviderResilienceExtensions
{
    public static IHttpClientBuilder AddFrankfurterResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("rate-provider", pipeline =>
        {
            // Retries transient failures only (5xx, 408, timeouts, connection errors) - a 400/404
            // for an unknown currency is not transient and retrying it would just waste time.
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome)),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200)
            });

            // Stops hammering the external API once it is clearly down, instead of every
            // in-flight request individually waiting out its own retries.
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome)),
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15)
            });
        });

        return builder;
    }
}
