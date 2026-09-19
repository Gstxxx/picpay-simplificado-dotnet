namespace PicPay.Domain.Transactions;

public sealed class Transaction
{
    private Transaction() { }

    public Transaction(Guid payerId, Guid payeeId, decimal value, string? idempotencyKey)
    {
        if (value <= 0)
            throw new DomainException("Transfer value must be positive.");
        if (payerId == payeeId)
            throw new DomainException("Payer and payee must be different users.");

        Id = Guid.CreateVersion7();
        PayerId = payerId;
        PayeeId = payeeId;
        Value = value;
        IdempotencyKey = idempotencyKey;
        CreatedAt = SystemTime.UtcNow();
    }

    public Guid Id { get; private set; }
    public Guid PayerId { get; private set; }
    public Guid PayeeId { get; private set; }
    public decimal Value { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
