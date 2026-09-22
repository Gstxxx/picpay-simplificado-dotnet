using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PicPay.Infrastructure.Notifications;

internal sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Outbox worker disabled");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(settings.PollingIntervalMilliseconds));

        do
        {
            try
            {
                int processed;
                do
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                    processed = await processor.ProcessBatchAsync(stoppingToken);
                } while (processed == settings.BatchSize && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox worker iteration failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
