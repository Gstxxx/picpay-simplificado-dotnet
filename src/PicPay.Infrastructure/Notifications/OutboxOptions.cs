using System.ComponentModel.DataAnnotations;
using PicPay.Domain.Notifications;

namespace PicPay.Infrastructure.Notifications;

public sealed class OutboxOptions
{
    public const string Section = "Outbox";

    public bool Enabled { get; init; } = true;

    [Range(50, 60_000)]
    public int PollingIntervalMilliseconds { get; init; } = 2_000;

    [Range(1, 500)]
    public int BatchSize { get; init; } = 20;

    [Range(1, 50)]
    public int MaxAttempts { get; init; } = 8;

    [Range(0.01, 3_600)]
    public double RetryBaseDelaySeconds { get; init; } = 5;

    [Range(0.01, 86_400)]
    public double RetryMaxDelaySeconds { get; init; } = 600;

    public RetrySchedule ToRetrySchedule() =>
        new(MaxAttempts, TimeSpan.FromSeconds(RetryBaseDelaySeconds), TimeSpan.FromSeconds(RetryMaxDelaySeconds));
}
