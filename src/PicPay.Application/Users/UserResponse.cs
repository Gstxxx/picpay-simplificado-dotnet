using PicPay.Domain.Users;

namespace PicPay.Application.Users;

public sealed record UserResponse(Guid Id, string FullName, string Email, UserType Type, decimal Balance)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.FullName, user.Email, user.Type, user.Balance);
}
