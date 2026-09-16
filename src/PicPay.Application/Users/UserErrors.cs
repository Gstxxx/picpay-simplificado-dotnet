using PicPay.Application.Common;

namespace PicPay.Application.Users;

public static class UserErrors
{
    public static readonly Error NotFound = new("user.not_found", "User not found.", ErrorType.NotFound);
    public static readonly Error AlreadyExists = new("user.already_exists", "Email or document already registered.", ErrorType.Conflict);
    public static readonly Error InvalidCredentials = new("auth.invalid_credentials", "Invalid email or password.", ErrorType.Unauthorized);
}
