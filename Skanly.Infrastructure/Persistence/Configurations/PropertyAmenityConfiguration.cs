using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
    {
        public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
        {
            builder.ToTable("PropertyAmenities");

            builder.HasKey(x => new { x.PropertyId, x.AmenityId });

            builder.HasOne(x => x.Property)
                   .WithMany(x => x.PropertyAmenities)
                   .HasForeignKey(x => x.PropertyId);

            builder.HasOne(x => x.Amenity)
                   .WithMany(x => x.PropertyAmenities)
                   .HasForeignKey(x => x.AmenityId);
        }
    }
}
