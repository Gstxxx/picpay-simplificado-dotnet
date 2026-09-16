namespace PicPay.Application.Common;

public sealed class UniqueConstraintException(string constraintName, Exception inner)
    : Exception($"Unique constraint '{constraintName}' violated.", inner)
{
    public string ConstraintName { get; } = constraintName;
}
