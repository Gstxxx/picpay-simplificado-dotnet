namespace PicPay.Application.Abstractions;

public interface INotificationSender
{
    Task SendAsync(string recipient, string message, CancellationToken ct);
}
