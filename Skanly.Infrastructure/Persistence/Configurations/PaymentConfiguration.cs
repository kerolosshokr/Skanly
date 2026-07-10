// Skanly.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.TransactionReference).HasMaxLength(100);
        builder.Property(p => p.PaymentMethod).HasConversion<byte>();
        builder.Property(p => p.Status).HasConversion<byte>();
    }
}