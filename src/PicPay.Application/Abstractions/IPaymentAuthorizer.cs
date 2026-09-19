namespace PicPay.Application.Abstractions;

public enum AuthorizationDecision
{
    Authorized,
    Denied,
    Unavailable
}

public interface IPaymentAuthorizer
{
    Task<AuthorizationDecision> AuthorizeAsync(CancellationToken ct);
}
