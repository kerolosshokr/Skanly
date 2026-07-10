using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;
namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
    {
        public void Configure(EntityTypeBuilder<Amenity> builder)
        {
            builder.ToTable("Amenities");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.NameAr)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.NameEn)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Icon)
                   .HasMaxLength(250);

            builder.Property(x => x.IsActive)
                   .HasDefaultValue(true);

            builder.HasIndex(x => x.NameEn)
                   .IsUnique();
        }
    }
}
