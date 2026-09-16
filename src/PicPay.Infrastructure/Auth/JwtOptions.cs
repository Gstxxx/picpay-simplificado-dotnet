using System.ComponentModel.DataAnnotations;

namespace PicPay.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    [Required, MinLength(32)]
    public string Secret { get; init; } = null!;

    [Range(1, 1440)]
    public int ExpirationMinutes { get; init; } = 60;
}
