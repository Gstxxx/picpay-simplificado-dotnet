using PicPay.Domain.Notifications;

namespace PicPay.UnitTests.Domain;

public class NotificationOutboxTests
{
    private static readonly RetrySchedule Schedule = new(MaxAttempts: 3, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_message_is_pending_and_due_immediately()
    {
        var message = new NotificationOutbox(Guid.NewGuid(), "a@b.dev", "oi");

        Assert.Equal(NotificationStatus.Pending, message.Status);
        Assert.Equal(message.CreatedAt, message.NextAttemptAt);
        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public void Failed_attempt_schedules_retry_with_backoff()
    {
        var message = new NotificationOutbox(Guid.NewGuid(), "a@b.dev", "oi");

        message.MarkAttemptFailed("timeout", Now, Schedule);
        Assert.Equal(Now.AddSeconds(5), message.NextAttemptAt);

        message.MarkAttemptFailed("timeout", Now, Schedule);
        Assert.Equal(Now.AddSeconds(10), message.NextAttemptAt);

        Assert.Equal(NotificationStatus.Pending, message.Status);
        Assert.Equal("timeout", message.LastError);
    }

    [Fact]
    public void Gives_up_after_max_attempts()
    {
        var message = new NotificationOutbox(Guid.NewGuid(), "a@b.dev", "oi");

        for (var i = 0; i < Schedule.MaxAttempts; i++)
            message.MarkAttemptFailed("boom", Now, Schedule);

        Assert.Equal(NotificationStatus.Failed, message.Status);
        Assert.Equal(3, message.Attempts);
    }

    [Fact]
    public void Mark_sent_clears_error()
    {
        var message = new NotificationOutbox(Guid.NewGuid(), "a@b.dev", "oi");
        message.MarkAttemptFailed("boom", Now, Schedule);

        message.MarkSent(Now.AddMinutes(1));

        Assert.Equal(NotificationStatus.Sent, message.Status);
        Assert.Equal(Now.AddMinutes(1), message.SentAt);
        Assert.Null(message.LastError);
        Assert.Equal(2, message.Attempts);
    }

    [Fact]
    public void Long_errors_are_truncated()
    {
        var message = new NotificationOutbox(Guid.NewGuid(), "a@b.dev", "oi");

        message.MarkAttemptFailed(new string('x', 2_000), Now, Schedule);

        Assert.Equal(500, message.LastError!.Length);
    }
}
