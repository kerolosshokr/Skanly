using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public  class AreaConfiguration : IEntityTypeConfiguration<Area>
    {
        public void Configure(EntityTypeBuilder<Area> builder)
        {
            builder.ToTable("Areas");

            builder.HasKey(x => x.Id);

            builder.Property(a => a.NameAr)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.NameEn)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(a => a.NameEn)
                .IsUnique();

        }
    }
}
