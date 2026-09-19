using PicPay.Domain.Users;

namespace PicPay.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> ExistsAsync(string email, string document, CancellationToken ct);
    void Add(User user);

    /// <summary>Debita só se houver saldo, numa única instrução UPDATE. Retorna false se o saldo não bastar.</summary>
    Task<bool> TryDebitAsync(Guid userId, decimal amount, CancellationToken ct);

    Task CreditAsync(Guid userId, decimal amount, CancellationToken ct);
}
