namespace PicPay.Domain.Notifications;

public sealed record RetrySchedule(int MaxAttempts, TimeSpan BaseDelay, TimeSpan MaxDelay)
{
    public TimeSpan DelayFor(int attempt)
    {
        var factor = Math.Pow(2, Math.Max(0, attempt - 1));
        var delay = TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * factor);
        return delay > MaxDelay ? MaxDelay : delay;
    }
}
