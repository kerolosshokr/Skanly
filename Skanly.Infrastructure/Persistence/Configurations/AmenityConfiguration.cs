// Skanly.Infrastructure/Persistence/Configurations/AmenityConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(a => a.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(a => a.IconClass).HasMaxLength(100);
        builder.HasIndex(a => a.NameEn).IsUnique();
    }
}