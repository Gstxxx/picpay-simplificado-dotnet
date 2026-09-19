using Microsoft.EntityFrameworkCore;
using PicPay.Application.Abstractions;
using PicPay.Domain.Transactions;

namespace PicPay.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Transaction?> GetByIdempotencyKeyAsync(Guid payerId, string key, CancellationToken ct) =>
        db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.PayerId == payerId && t.IdempotencyKey == key, ct);

    public void Add(Transaction transaction) => db.Transactions.Add(transaction);
}
