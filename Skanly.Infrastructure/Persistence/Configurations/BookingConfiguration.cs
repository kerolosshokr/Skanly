using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CheckInDate)
                   .IsRequired();

            builder.Property(x => x.CheckOutDate)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .IsRequired();

            builder.HasOne(x => x.Student)
                   .WithMany(x => x.Bookings)
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Property)
                   .WithMany(x => x.Bookings)
                   .HasForeignKey(x => x.PropertyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}