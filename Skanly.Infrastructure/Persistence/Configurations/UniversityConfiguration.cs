// Skanly.Infrastructure/Persistence/Configurations/UniversityConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.ToTable("Universities");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(u => u.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Address).HasMaxLength(300);
        builder.Property(u => u.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(u => u.Longitude).HasColumnType("decimal(9,6)");
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        builder.HasIndex(u => u.NameEn).IsUnique();

        builder.HasMany(u => u.Students)
            .WithOne(s => s.University)
            .HasForeignKey(s => s.UniversityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.Properties)
            .WithOne(p => p.University)
            .HasForeignKey(p => p.UniversityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}