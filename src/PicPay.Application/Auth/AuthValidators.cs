using FluentValidation;
using PicPay.Domain.Users;

namespace PicPay.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.FullName).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(r => r.Password).NotEmpty().MinimumLength(8).MaximumLength(72);
        RuleFor(r => r.Type).IsInEnum();
        RuleFor(r => r.Document).NotEmpty().Matches("^[0-9]+$").WithMessage("Document must contain only digits.");
        RuleFor(r => r.Document).Length(11).When(r => r.Type == UserType.Common).WithMessage("CPF must have 11 digits.");
        RuleFor(r => r.Document).Length(14).When(r => r.Type == UserType.Merchant).WithMessage("CNPJ must have 14 digits.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress();
        RuleFor(r => r.Password).NotEmpty();
    }
}
