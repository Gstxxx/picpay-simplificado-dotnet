namespace PicPay.Domain.Notifications;

public sealed class NotificationOutbox
{
    private NotificationOutbox() { }

    public NotificationOutbox(Guid transactionId, string recipient, string message)
    {
        Id = Guid.CreateVersion7();
        TransactionId = transactionId;
        Recipient = recipient;
        Message = message;
        Status = NotificationStatus.Pending;
        CreatedAt = SystemTime.UtcNow();
        NextAttemptAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public string Recipient { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public NotificationStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    public void MarkSent(DateTimeOffset now)
    {
        Attempts++;
        Status = NotificationStatus.Sent;
        SentAt = now;
        LastError = null;
    }

    public void MarkAttemptFailed(string error, DateTimeOffset now, RetrySchedule schedule)
    {
        Attempts++;
        LastError = error.Length > 500 ? error[..500] : error;

        if (Attempts >= schedule.MaxAttempts)
        {
            Status = NotificationStatus.Failed;
            return;
        }

        NextAttemptAt = now + schedule.DelayFor(Attempts);
    }
}
