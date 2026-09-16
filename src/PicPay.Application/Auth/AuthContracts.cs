using PicPay.Domain.Users;

namespace PicPay.Application.Auth;

public sealed record RegisterRequest(string FullName, string Document, string Email, string Password, UserType Type);

public sealed record LoginRequest(string Email, string Password);

public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
