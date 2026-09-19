using System.Globalization;
using PicPay.Application.Abstractions;
using PicPay.Application.Common;
using PicPay.Domain.Notifications;
using PicPay.Domain.Transactions;
using PicPay.Domain.Users;

namespace PicPay.Application.Transfers;

public sealed class TransferService(
    IUserRepository users,
    ITransactionRepository transactions,
    INotificationOutboxRepository outbox,
    IPaymentAuthorizer authorizer,
    IUnitOfWork unitOfWork)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<Result<TransferResult>> TransferAsync(TransferCommand command, CancellationToken ct)
    {
        var payerId = command.AuthenticatedUserId;
        var request = command.Request;

        if (request.Payer is { } informedPayer && informedPayer != payerId)
            return TransferErrors.PayerMismatch;

        if (request.Payee == payerId)
            return TransferErrors.SelfTransfer;

        if (command.IdempotencyKey is { } key &&
            await transactions.GetByIdempotencyKeyAsync(payerId, key, ct) is { } existing)
            return Replay(existing, request);

        var payer = await users.GetByIdAsync(payerId, ct);
        if (payer is null)
            return TransferErrors.PayerNotFound;
        if (!payer.CanSendMoney)
            return TransferErrors.MerchantCannotSend;
        if (payer.Balance < request.Value)
            return TransferErrors.InsufficientFunds;

        var payee = await users.GetByIdAsync(request.Payee, ct);
        if (payee is null)
            return TransferErrors.PayeeNotFound;

        switch (await authorizer.AuthorizeAsync(ct))
        {
            case AuthorizationDecision.Denied:
                return TransferErrors.NotAuthorized;
            case AuthorizationDecision.Unavailable:
                return TransferErrors.AuthorizerUnavailable;
        }

        var transaction = new Transaction(payer.Id, payee.Id, request.Value, command.IdempotencyKey);

        return await ExecuteAsync(transaction, payer, payee, ct)
            ?? await ReplayAfterRaceAsync(payerId, command.IdempotencyKey!, request, ct);
    }

    public async Task<Result<TransferResponse>> GetAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var transaction = await transactions.GetByIdAsync(id, ct);
        if (transaction is null || (transaction.PayerId != userId && transaction.PayeeId != userId))
            return TransferErrors.NotFound;

        return TransferResponse.From(transaction);
    }

    /// <summary>
    /// Retorna null quando outra requisição com a mesma Idempotency-Key gravou primeiro.
    /// A transação é inserida antes do débito: a requisição concorrente fica bloqueada no índice
    /// único até a primeira terminar e então falha, sem nunca tocar no saldo.
    /// </summary>
    private async Task<Result<TransferResult>?> ExecuteAsync(Transaction transaction, User payer, User payee, CancellationToken ct)
    {
        await using var scope = await unitOfWork.BeginTransactionAsync(ct);

        transactions.Add(transaction);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException) when (transaction.IdempotencyKey is not null)
        {
            return null;
        }

        if (!await users.TryDebitAsync(payer.Id, transaction.Value, ct))
            return TransferErrors.InsufficientFunds;

        await users.CreditAsync(payee.Id, transaction.Value, ct);

        outbox.Add(new NotificationOutbox(
            transaction.Id,
            payee.Email,
            $"Você recebeu {transaction.Value.ToString("C", PtBr)} de {payer.FullName}."));

        await unitOfWork.SaveChangesAsync(ct);
        await scope.CommitAsync(ct);

        return new TransferResult(TransferResponse.From(transaction), Replayed: false);
    }

    private async Task<Result<TransferResult>> ReplayAfterRaceAsync(Guid payerId, string key, TransferRequest request, CancellationToken ct)
    {
        var existing = await transactions.GetByIdempotencyKeyAsync(payerId, key, ct)
            ?? throw new InvalidOperationException("Unique violation on idempotency key but no transaction was found.");

        return Replay(existing, request);
    }

    private static Result<TransferResult> Replay(Transaction existing, TransferRequest request)
    {
        if (existing.PayeeId != request.Payee || existing.Value != request.Value)
            return TransferErrors.IdempotencyKeyReused;

        return new TransferResult(TransferResponse.From(existing), Replayed: true);
    }
}
