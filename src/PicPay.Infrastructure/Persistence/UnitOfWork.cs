using Microsoft.EntityFrameworkCore;
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
}
