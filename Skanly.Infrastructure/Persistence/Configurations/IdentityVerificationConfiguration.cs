using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class IdentityVerificationConfiguration : IEntityTypeConfiguration<IdentityVerification>
    {
        public void Configure(EntityTypeBuilder<IdentityVerification> builder)
        {
            builder.ToTable("IdentityVerifications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DocumentUrl)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(x => x.Status)
                   .HasConversion<byte>();

            builder.HasOne(x => x.Student)
                   .WithMany(x => x.IdentityVerifications)
                   .HasForeignKey(x => x.StudentId);

            builder.HasOne(x => x.ReviewedByAdmin)
                   .WithMany(x => x.ReviewedIdentityVerifications)
                   .HasForeignKey(x => x.ReviewedByAdminId)
                   .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
