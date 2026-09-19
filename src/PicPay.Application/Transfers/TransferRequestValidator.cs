using FluentValidation;

namespace PicPay.Application.Transfers;

public sealed class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(r => r.Value).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(r => r.Payee).NotEmpty();
    }
}
