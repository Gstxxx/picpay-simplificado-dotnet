namespace PicPay.Application.Common;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Unprocessable,
    Unavailable
}

public sealed record Error(string Code, string Message, ErrorType Type);
