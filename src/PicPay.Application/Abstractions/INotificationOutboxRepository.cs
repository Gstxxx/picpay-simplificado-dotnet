using PicPay.Domain.Notifications;

namespace PicPay.Application.Abstractions;

public interface INotificationOutboxRepository
{
    void Add(NotificationOutbox message);
}
