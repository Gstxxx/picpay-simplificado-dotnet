using PicPay.Application.Common;

namespace PicPay.Api.Infrastructure;

public static class ErrorResults
{
    public static IResult ToProblem(this Error error) =>
        TypedResults.Problem(
            statusCode: error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unprocessable => StatusCodes.Status422UnprocessableEntity,
                ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            },
            title: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
