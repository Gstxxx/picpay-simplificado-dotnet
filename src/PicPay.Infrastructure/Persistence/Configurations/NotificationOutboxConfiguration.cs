using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PicPay.Domain.Notifications;
using PicPay.Domain.Transactions;

namespace PicPay.Infrastructure.Persistence.Configurations;

internal sealed class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("notification_outbox");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Recipient).HasMaxLength(254).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.LastError).HasMaxLength(500);

        builder.HasOne<Transaction>().WithMany().HasForeignKey(n => n.TransactionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.Status, n.NextAttemptAt });
    }
}
