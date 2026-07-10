using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(x => x.PaymentMethod)
                   .IsRequired();

            builder.Property(x => x.PaymentDate)
                   .IsRequired();

            builder.HasOne(x => x.Booking)
                   .WithMany(x => x.Payments)
                   .HasForeignKey(x => x.BookingId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}