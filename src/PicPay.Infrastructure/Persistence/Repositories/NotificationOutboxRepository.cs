using PicPay.Application.Abstractions;
using PicPay.Domain.Notifications;

namespace PicPay.Infrastructure.Persistence.Repositories;

internal sealed class NotificationOutboxRepository(AppDbContext db) : INotificationOutboxRepository
{
    public void Add(NotificationOutbox message) => db.NotificationOutbox.Add(message);
}
