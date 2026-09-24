using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace PicPay.Api.Infrastructure;

public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    [Range(1, 10_000)]
    public int AuthPermitLimit { get; init; } = 10;

    [Range(1, 3_600)]
    public int AuthWindowSeconds { get; init; } = 60;

    [Range(1, 10_000)]
    public int TransferBurst { get; init; } = 20;

    [Range(1, 10_000)]
    public int TransferTokensPerMinute { get; init; } = 30;
}

public static class RateLimitingSetup
{
    public const string AuthPolicy = "auth";
    public const string TransferPolicy = "transfer";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                await TypedResults.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Too many requests.")
                    .ExecuteAsync(context.HttpContext);
            };

            limiter.AddPolicy(AuthPolicy, http =>
            {
                var options = Settings(http);
                return RateLimitPartition.GetFixedWindowLimiter(ClientIp(http), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(options.AuthWindowSeconds),
                    QueueLimit = 0
                });
            });

            limiter.AddPolicy(TransferPolicy, http =>
            {
                var options = Settings(http);
                var partition = http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? ClientIp(http);
                return RateLimitPartition.GetTokenBucketLimiter(partition, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = options.TransferBurst,
                    TokensPerPeriod = options.TransferTokensPerMinute,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        return services;
    }

    private static RateLimitingOptions Settings(HttpContext http) =>
        http.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static string ClientIp(HttpContext http) =>
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
