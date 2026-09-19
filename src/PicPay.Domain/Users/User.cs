namespace PicPay.Domain.Users;

public sealed class User
{
    private User() { }

    public User(string fullName, string document, string email, string passwordHash, UserType type)
    {
        Id = Guid.CreateVersion7();
        FullName = fullName;
        Document = document;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        Type = type;
        CreatedAt = SystemTime.UtcNow();
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = null!;
    public string Document { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserType Type { get; private set; }
    public decimal Balance { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool CanSendMoney => Type == UserType.Common;

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Deposit amount must be positive.");

        Balance += amount;
    }
}
