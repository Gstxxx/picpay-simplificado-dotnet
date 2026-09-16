using PicPay.Api.Infrastructure;
using PicPay.Application.Auth;
using PicPay.Application.Users;

namespace PicPay.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
            {
                var result = await auth.RegisterAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/users/{result.Value.Id}", result.Value)
                    : result.Error!.ToProblem();
            })
            .WithValidation<RegisterRequest>()
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
            {
                var result = await auth.LoginAsync(request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
            })
            .WithValidation<LoginRequest>()
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
