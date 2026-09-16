using PicPay.Application.Abstractions;
using PicPay.Application.Common;
using PicPay.Application.Users;
using PicPay.Domain.Users;

namespace PicPay.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    ITokenService tokens)
{
    public async Task<Result<UserResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (await users.ExistsAsync(request.Email.ToLowerInvariant(), request.Document, ct))
            return UserErrors.AlreadyExists;

        var user = new User(request.FullName, request.Document, request.Email, hasher.Hash(request.Password), request.Type);
        users.Add(user);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            return UserErrors.AlreadyExists;
        }

        return UserResponse.From(user);
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.ToLowerInvariant(), ct);

        if (!hasher.Verify(request.Password, user?.PasswordHash) || user is null)
            return UserErrors.InvalidCredentials;

        var token = tokens.Issue(user);
        return new TokenResponse(token.Token, "Bearer", token.ExpiresAt);
    }
}
