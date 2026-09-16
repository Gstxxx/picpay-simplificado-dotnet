namespace PicPay.Application.Common;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Error = error;
    }

    public bool IsSuccess { get; }
    public Error? Error { get; }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Result has no value.");

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}
