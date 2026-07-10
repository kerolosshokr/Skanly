// Skanly.Infrastructure/Persistence/Configurations/AreaConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("Areas");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(a => a.NameEn).IsRequired().HasMaxLength(100);
        builder.HasIndex(a => a.NameEn).IsUnique();

        builder.HasMany(a => a.Properties)
            .WithOne(p => p.Area)
            .HasForeignKey(p => p.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}