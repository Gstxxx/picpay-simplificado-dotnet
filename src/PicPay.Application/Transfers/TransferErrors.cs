using PicPay.Application.Common;

namespace PicPay.Application.Transfers;

public static class TransferErrors
{
    public static readonly Error PayerMismatch = new("transfer.payer_mismatch", "Payer must be the authenticated user.", ErrorType.Forbidden);
    public static readonly Error SelfTransfer = new("transfer.self_transfer", "Payer and payee must be different users.", ErrorType.Validation);
    public static readonly Error PayerNotFound = new("transfer.payer_not_found", "Payer not found.", ErrorType.NotFound);
    public static readonly Error PayeeNotFound = new("transfer.payee_not_found", "Payee not found.", ErrorType.NotFound);
    public static readonly Error MerchantCannotSend = new("transfer.merchant_cannot_send", "Merchants can only receive transfers.", ErrorType.Forbidden);
    public static readonly Error InsufficientFunds = new("transfer.insufficient_funds", "Insufficient balance.", ErrorType.Unprocessable);
    public static readonly Error NotAuthorized = new("transfer.not_authorized", "Transfer denied by the external authorizer.", ErrorType.Forbidden);
    public static readonly Error AuthorizerUnavailable = new("transfer.authorizer_unavailable", "Authorization service is unavailable. Try again later.", ErrorType.Unavailable);
    public static readonly Error IdempotencyKeyReused = new("transfer.idempotency_key_reused", "Idempotency-Key was already used with a different payload.", ErrorType.Unprocessable);
    public static readonly Error NotFound = new("transfer.not_found", "Transfer not found.", ErrorType.NotFound);
}
