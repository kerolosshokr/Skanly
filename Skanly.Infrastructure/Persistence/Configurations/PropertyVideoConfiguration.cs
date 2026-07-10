using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class PropertyVideoConfiguration : IEntityTypeConfiguration<PropertyVideo>
    {
        public void Configure(EntityTypeBuilder<PropertyVideo> builder)
        {
            builder.ToTable("PropertyVideos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.VideoUrl)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.HasOne(x => x.Property)
                   .WithMany()
                   .HasForeignKey(x => x.PropertyId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
