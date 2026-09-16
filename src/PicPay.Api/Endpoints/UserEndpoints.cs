using System.Security.Claims;
using PicPay.Api.Infrastructure;
using PicPay.Application.Users;

namespace PicPay.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users/me", async (ClaimsPrincipal principal, UserService users, CancellationToken ct) =>
            {
                var result = await users.GetAsync(principal.GetUserId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
            })
            .WithTags("Users")
            .RequireAuthorization()
            .Produces<UserResponse>();

        return app;
    }
}
