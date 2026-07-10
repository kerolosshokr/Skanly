using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class UniversityConfiguration : IEntityTypeConfiguration<University>
    {
        public void Configure(EntityTypeBuilder<University> builder)
        {
            builder.ToTable("Universities");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.NameAr)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.NameEn)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.Address)
                   .HasMaxLength(250);

            builder.Property(x => x.Latitude)
                   .HasColumnType("decimal(9,6)");

            builder.Property(x => x.Longitude)
                   .HasColumnType("decimal(9,6)");

            builder.Property(x => x.IsActive)
                   .HasDefaultValue(true);


            builder.HasIndex(x => x.NameEn)
                   .IsUnique();


        }
    }
}