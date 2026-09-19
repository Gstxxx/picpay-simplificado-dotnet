using PicPay.Domain.Transactions;

namespace PicPay.Application.Transfers;

public sealed record TransferRequest(decimal Value, Guid Payee, Guid? Payer = null);

public sealed record TransferCommand(Guid AuthenticatedUserId, TransferRequest Request, string? IdempotencyKey);

public sealed record TransferResponse(Guid Id, Guid Payer, Guid Payee, decimal Value, DateTimeOffset CreatedAt)
{
    public static TransferResponse From(Transaction t) => new(t.Id, t.PayerId, t.PayeeId, t.Value, t.CreatedAt);
}

public sealed record TransferResult(TransferResponse Transfer, bool Replayed);
