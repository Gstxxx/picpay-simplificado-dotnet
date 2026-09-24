using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PicPay.Api.Infrastructure;
using PicPay.Application.Transfers;

namespace PicPay.Api.Endpoints;

public static class TransferEndpoints
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private const int MaxIdempotencyKeyLength = 100;

    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/transfer").WithTags("Transfers").RequireAuthorization();

        group.MapPost("/", async (
                TransferRequest request,
                [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
                ClaimsPrincipal principal,
                TransferService transfers,
                HttpContext http,
                CancellationToken ct) =>
            {
                if (idempotencyKey is not null && (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength))
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        [IdempotencyHeader] = [$"Must be a non-empty string up to {MaxIdempotencyKeyLength} characters."]
                    });
                }

                var result = await transfers.TransferAsync(new TransferCommand(principal.GetUserId(), request, idempotencyKey), ct);
                if (!result.IsSuccess)
                    return result.Error!.ToProblem();

                var (transfer, replayed) = result.Value;
                if (replayed)
                {
                    http.Response.Headers["Idempotent-Replayed"] = "true";
                    return Results.Ok(transfer);
                }

                return Results.Created($"/transfer/{transfer.Id}", transfer);
            })
            .RequireRateLimiting(RateLimitingSetup.TransferPolicy)
            .WithValidation<TransferRequest>()
            .Produces<TransferResponse>(StatusCodes.Status201Created)
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, TransferService transfers, CancellationToken ct) =>
            {
                var result = await transfers.GetAsync(id, principal.GetUserId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
            })
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
