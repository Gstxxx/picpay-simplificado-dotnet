using PicPay.Application.Abstractions;

namespace PicPay.Infrastructure.Auth;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor);

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string? hash)
    {
        if (hash is null)
        {
            BCrypt.Net.BCrypt.Verify(password, DummyHash);
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
