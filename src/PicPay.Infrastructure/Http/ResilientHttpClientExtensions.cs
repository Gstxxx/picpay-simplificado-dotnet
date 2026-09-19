using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace PicPay.Infrastructure.Http;

internal static class ResilientHttpClientExtensions
{
    public static IHttpClientBuilder AddResilientClient<TClient, TImplementation, TOptions>(
        this IServiceCollection services, string section)
        where TClient : class
        where TImplementation : class, TClient
        where TOptions : ExternalServiceOptions
    {
        services.AddOptions<TOptions>()
            .BindConfiguration(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var builder = services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<TOptions>>().Value.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        builder.AddStandardResilienceHandler().Configure((HttpStandardResilienceOptions resilience, IServiceProvider sp) =>
        {
            var options = sp.GetRequiredService<IOptions<TOptions>>().Value;
            var attemptTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            resilience.AttemptTimeout.Timeout = attemptTimeout;
            resilience.Retry.MaxRetryAttempts = options.MaxRetries;
            resilience.Retry.Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds);
            resilience.Retry.BackoffType = DelayBackoffType.Exponential;
            resilience.Retry.UseJitter = true;
            resilience.TotalRequestTimeout.Timeout = attemptTimeout * (options.MaxRetries + 1) + TimeSpan.FromSeconds(5);
            resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2 + TimeSpan.FromSeconds(30);
        });

        return builder;
    }
}
