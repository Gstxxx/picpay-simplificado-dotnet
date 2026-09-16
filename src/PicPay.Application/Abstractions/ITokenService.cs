using PicPay.Domain.Users;

namespace PicPay.Application.Abstractions;

public interface ITokenService
{
    AccessToken Issue(User user);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
