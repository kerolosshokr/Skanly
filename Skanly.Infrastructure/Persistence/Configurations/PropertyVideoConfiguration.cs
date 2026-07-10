// Skanly.Infrastructure/Persistence/Configurations/PropertyVideoConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class PropertyVideoConfiguration : IEntityTypeConfiguration<PropertyVideo>
{
    public void Configure(EntityTypeBuilder<PropertyVideo> builder)
    {
        builder.ToTable("PropertyVideos");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VideoUrl).IsRequired().HasMaxLength(300);
    }
}