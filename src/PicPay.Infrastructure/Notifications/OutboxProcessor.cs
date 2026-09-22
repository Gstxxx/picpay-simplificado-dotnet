using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PicPay.Application.Abstractions;
using PicPay.Domain.Notifications;
using PicPay.Infrastructure.Persistence;

namespace PicPay.Infrastructure.Notifications;

public sealed class OutboxProcessor(
    AppDbContext db,
    INotificationSender sender,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxProcessor> logger)
{
    /// <summary>
    /// Processa um lote de mensagens pendentes. FOR UPDATE SKIP LOCKED permite rodar
    /// várias instâncias da API sem que duas enviem a mesma notificação.
    /// </summary>
    public async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        var settings = options.Value;
        var schedule = settings.ToRetrySchedule();
        var now = clock.GetUtcNow();
        var pending = nameof(NotificationStatus.Pending);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var batch = await db.NotificationOutbox
            .FromSql($"""
                SELECT * FROM notification_outbox
                WHERE status = {pending} AND next_attempt_at <= {now}
                ORDER BY next_attempt_at
                LIMIT {settings.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            try
            {
                await sender.SendAsync(message.Recipient, message.Message, ct);
                message.MarkSent(clock.GetUtcNow());
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                message.MarkAttemptFailed(ex.Message, clock.GetUtcNow(), schedule);
                logger.LogWarning(ex, "Notification {NotificationId} failed (attempt {Attempt}, status {Status})",
                    message.Id, message.Attempts, message.Status);
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return batch.Count;
    }
}
