using PicPay.Application.Auth;
using PicPay.Domain.Users;

namespace PicPay.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Theory]
    [InlineData(UserType.Common, "12345678909")]
    [InlineData(UserType.Merchant, "11222333000181")]
    public void Accepts_valid_requests(UserType type, string document)
    {
        var result = _validator.Validate(new RegisterRequest("Fulano", document, "f@test.dev", "senha1234", type));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(UserType.Common, "11222333000181")]
    [InlineData(UserType.Merchant, "12345678909")]
    [InlineData(UserType.Common, "123.456.789-09")]
    public void Rejects_document_that_does_not_match_user_type(UserType type, string document)
    {
        var result = _validator.Validate(new RegisterRequest("Fulano", document, "f@test.dev", "senha1234", type));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Document));
    }

    [Fact]
    public void Rejects_short_password_and_invalid_email()
    {
        var result = _validator.Validate(new RegisterRequest("Fulano", "12345678909", "not-an-email", "123", UserType.Common));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Email));
    }
}
