using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PicPay.Domain.Users;

namespace PicPay.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t => t.HasCheckConstraint("ck_users_balance_non_negative", "balance >= 0"));
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Document).HasMaxLength(14).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.Balance).HasPrecision(18, 2);

        builder.HasIndex(u => u.Document).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
