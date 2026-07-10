using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class PropertyConfiguration : IEntityTypeConfiguration<Property>
    {
        public void Configure(EntityTypeBuilder<Property> builder)
        {
            builder.ToTable("Properties");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.Description)
                   .IsRequired()
                   .HasMaxLength(2000);

            builder.Property(x => x.Address)
                   .IsRequired()
                   .HasMaxLength(300);

            builder.Property(x => x.PricePerMonth)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Latitude)
                   .HasColumnType("decimal(9,6)");

            builder.Property(x => x.Longitude)
                   .HasColumnType("decimal(9,6)");

            builder.Property(x => x.PropertyType)
                   .HasConversion<byte>();

            builder.Property(x => x.IsApproved)
                   .HasDefaultValue(false);

            builder.Property(x => x.IsAvailable)
                   .HasDefaultValue(true);

            builder.Property(x => x.IsDeleted)
                   .HasDefaultValue(false);

            // Owner
            builder.HasOne(x => x.Owner)
                   .WithMany(x => x.Properties)
                   .HasForeignKey(x => x.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Area
            builder.HasOne(x => x.Area)
                   .WithMany(x => x.Properties)
                   .HasForeignKey(x => x.AreaId)
                   .OnDelete(DeleteBehavior.Restrict);

            // University (Optional)
            builder.HasOne(x => x.University)
                   .WithMany(x => x.Properties)
                   .HasForeignKey(x => x.UniversityId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}