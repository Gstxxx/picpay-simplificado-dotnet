using PicPay.Domain;
using PicPay.Domain.Transactions;

namespace PicPay.UnitTests.Domain;

public class TransactionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Value_must_be_positive(decimal value)
    {
        Assert.Throws<DomainException>(() => new Transaction(Guid.NewGuid(), Guid.NewGuid(), value, null));
    }

    [Fact]
    public void Payer_and_payee_must_differ()
    {
        var id = Guid.NewGuid();

        Assert.Throws<DomainException>(() => new Transaction(id, id, 10, null));
    }

    [Fact]
    public void Keeps_idempotency_key()
    {
        var transaction = new Transaction(Guid.NewGuid(), Guid.NewGuid(), 10, "key-1");

        Assert.Equal("key-1", transaction.IdempotencyKey);
    }
}
