using PicPay.Domain.Transactions;

namespace PicPay.Application.Abstractions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Transaction?> GetByIdempotencyKeyAsync(Guid payerId, string key, CancellationToken ct);
    void Add(Transaction transaction);
}
