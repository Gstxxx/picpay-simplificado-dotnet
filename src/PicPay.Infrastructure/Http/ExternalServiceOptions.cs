using System.ComponentModel.DataAnnotations;

namespace PicPay.Infrastructure.Http;

public abstract class ExternalServiceOptions
{
    [Required, Url]
    public string BaseUrl { get; init; } = null!;

    [Required]
    public string Path { get; init; } = null!;

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 3;

    [Range(0, 10)]
    public int MaxRetries { get; init; } = 3;

    [Range(1, 10_000)]
    public int RetryDelayMilliseconds { get; init; } = 200;
}

public sealed class AuthorizerOptions : ExternalServiceOptions
{
    public const string Section = "Authorizer";
}
