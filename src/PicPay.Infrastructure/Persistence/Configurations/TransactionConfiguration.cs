using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PicPay.Domain.Transactions;
using PicPay.Domain.Users;

namespace PicPay.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public const string IdempotencyIndex = "ix_transactions_payer_id_idempotency_key";

    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", t => t.HasCheckConstraint("ck_transactions_value_positive", "value > 0"));
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Value).HasPrecision(18, 2);
        builder.Property(t => t.IdempotencyKey).HasMaxLength(100);

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.PayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.PayeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.PayerId, t.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName(IdempotencyIndex);
    }
}
