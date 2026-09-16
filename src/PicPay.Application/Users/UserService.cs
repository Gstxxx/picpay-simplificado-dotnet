using PicPay.Application.Abstractions;
using PicPay.Application.Common;

namespace PicPay.Application.Users;

public sealed class UserService(IUserRepository users)
{
    public async Task<Result<UserResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct);
        return user is null ? UserErrors.NotFound : UserResponse.From(user);
    }
}
