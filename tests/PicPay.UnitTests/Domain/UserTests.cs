using PicPay.Domain;
using PicPay.Domain.Users;

namespace PicPay.UnitTests.Domain;

public class UserTests
{
    [Theory]
    [InlineData(UserType.Common, true)]
    [InlineData(UserType.Merchant, false)]
    public void Only_common_users_can_send_money(UserType type, bool expected)
    {
        var user = new User("Fulano", "12345678909", "fulano@test.dev", "hash", type);

        Assert.Equal(expected, user.CanSendMoney);
    }

    [Fact]
    public void Email_is_normalized_to_lowercase()
    {
        var user = new User("Fulano", "12345678909", "Fulano@Test.DEV", "hash", UserType.Common);

        Assert.Equal("fulano@test.dev", user.Email);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_rejects_non_positive_amounts(decimal amount)
    {
        var user = new User("Fulano", "12345678909", "fulano@test.dev", "hash", UserType.Common);

        Assert.Throws<DomainException>(() => user.Deposit(amount));
    }
}
