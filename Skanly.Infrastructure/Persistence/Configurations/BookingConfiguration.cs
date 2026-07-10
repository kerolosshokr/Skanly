// Skanly.Infrastructure/Persistence/Configurations/BookingConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.StudentId).IsRequired().HasMaxLength(450);
        builder.Property(b => b.Status).HasConversion<byte>();
        builder.Property(b => b.TotalAmount).HasColumnType("decimal(10,2)");
        builder.Property(b => b.DepositAmount).HasColumnType("decimal(10,2)");
        builder.Property(b => b.CommissionRate).HasColumnType("decimal(5,2)").HasDefaultValue(10.00m);
        builder.Property(b => b.CommissionAmount).HasColumnType("decimal(10,2)");

        builder.HasIndex(b => b.StudentId);
        builder.HasIndex(b => b.PropertyId);
        builder.HasIndex(b => b.Status);

        builder.HasOne(b => b.Student)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Property)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Payments)
            .WithOne(p => p.Booking)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Contract)
            .WithOne(c => c.Booking)
            .HasForeignKey<Contract>(c => c.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Review)
            .WithOne(r => r.Booking)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}