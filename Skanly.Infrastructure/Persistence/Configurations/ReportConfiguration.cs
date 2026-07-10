using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.ToTable("Reports");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reason)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(x => x.ReportType)
                   .HasConversion<byte>();

            builder.Property(x => x.IsResolved)
                   .HasDefaultValue(false);

            builder.HasOne(x => x.Property)
                   .WithMany(x => x.Reports)
                   .HasForeignKey(x => x.PropertyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}