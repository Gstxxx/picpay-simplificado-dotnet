using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace PicPay.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("Authenticated principal has no valid 'sub' claim.");
}
