using PicPay.Application.Transfers;

namespace PicPay.UnitTests.Validators;

public class TransferRequestValidatorTests
{
    private readonly TransferRequestValidator _validator = new();

    [Theory]
    [InlineData(0.01)]
    [InlineData(100)]
    [InlineData(99.90)]
    public void Accepts_positive_values_with_up_to_two_decimals(decimal value)
    {
        Assert.True(_validator.Validate(new TransferRequest(value, Guid.NewGuid())).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.001)]
    public void Rejects_invalid_values(decimal value)
    {
        Assert.False(_validator.Validate(new TransferRequest(value, Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Rejects_empty_payee()
    {
        Assert.False(_validator.Validate(new TransferRequest(10, Guid.Empty)).IsValid);
    }
}
