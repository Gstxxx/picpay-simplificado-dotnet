using Microsoft.EntityFrameworkCore;
using PicPay.Application.Abstractions;
using PicPay.Domain.Users;

namespace PicPay.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExistsAsync(string email, string document, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Email == email || u.Document == document, ct);

    public void Add(User user) => db.Users.Add(user);
}
