using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PicPay.Application.Abstractions;
using PicPay.Application.Common;

namespace PicPay.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            db.ChangeTracker.Clear();
            throw new UniqueConstraintException(pg.ConstraintName ?? "unknown", ex);
        }
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct) =>
        new EfTransactionScope(await db.Database.BeginTransactionAsync(ct));

    private sealed class EfTransactionScope(IDbContextTransaction transaction) : ITransactionScope
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
