using Microsoft.EntityFrameworkCore;
using PicPay.Application.Abstractions;
using PicPay.Domain.Users;

namespace PicPay.Infrastructure.Persistence;

public static class DevelopmentSeeder
{
    public const string Password = "senha1234";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
            return;

        var hash = hasher.Hash(Password);

        var alice = new User("Alice Souza", "12345678909", "alice@picpay.dev", hash, UserType.Common);
        alice.Deposit(1_000m);

        var bob = new User("Bruno Lima", "98765432100", "bruno@picpay.dev", hash, UserType.Common);
        bob.Deposit(250m);

        var store = new User("Mercado Central", "11222333000181", "loja@picpay.dev", hash, UserType.Merchant);

        db.Users.AddRange(alice, bob, store);
        await db.SaveChangesAsync(ct);
    }
}
