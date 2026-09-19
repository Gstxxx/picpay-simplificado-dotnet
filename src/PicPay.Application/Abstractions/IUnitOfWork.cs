namespace PicPay.Application.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct);
}

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
