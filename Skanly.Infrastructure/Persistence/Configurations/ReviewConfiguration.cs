// Skanly.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(r => r.Id);
        // Skanly.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs — add
        builder.Property(r => r.IsHidden).HasDefaultValue(false);

        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.HasIndex(r => r.BookingId).IsUnique();
        builder.HasIndex(r => r.PropertyId);

        builder.HasOne(r => r.Student)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Property)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}