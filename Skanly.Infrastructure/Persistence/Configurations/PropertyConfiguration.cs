// Skanly.Infrastructure/Persistence/Configurations/PropertyConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OwnerId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Address).IsRequired().HasMaxLength(300);
        builder.Property(p => p.PricePerMonth).HasColumnType("decimal(10,2)");
        builder.Property(p => p.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(p => p.Longitude).HasColumnType("decimal(9,6)");
        builder.Property(p => p.AverageRating).HasColumnType("decimal(3,2)").HasDefaultValue(0);
        builder.Property(p => p.PropertyType).HasConversion<byte>();
        builder.Property(p => p.IsApproved).HasDefaultValue(false);
        builder.Property(p => p.IsAvailable).HasDefaultValue(true);
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);

        // Query filter — soft delete, automatically excluded everywhere unless overridden
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.AreaId);
        builder.HasIndex(p => p.UniversityId);
        builder.HasIndex(p => p.OwnerId);
 
        builder.HasIndex(p => p.PricePerMonth);
        builder.HasIndex(p => new { p.Latitude, p.Longitude }).HasDatabaseName("IX_Property_Geo");

        builder.HasOne(p => p.Owner)
            .WithMany(o => o.Properties)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.University)
            .WithMany(u => u.Properties)
            .HasForeignKey(p => p.UniversityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Area)
            .WithMany(a => a.Properties)
            .HasForeignKey(p => p.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Property)
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Videos)
            .WithOne(v => v.Property)
            .HasForeignKey(v => v.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Bookings)
            .WithOne(b => b.Property)
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Favorites)
            .WithOne(f => f.Property)
            .HasForeignKey(f => f.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Reviews)
            .WithOne(r => r.Property)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Reports)
            .WithOne(r => r.ReportedProperty)
            .HasForeignKey(r => r.ReportedPropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}