// Skanly.Infrastructure/Persistence/Configurations/IdentityVerificationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class IdentityVerificationConfiguration : IEntityTypeConfiguration<IdentityVerification>
{
    public void Configure(EntityTypeBuilder<IdentityVerification> builder)
    {
        builder.ToTable("IdentityVerifications");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.UserId).IsRequired().HasMaxLength(450);
        builder.Property(v => v.NationalIdFrontUrl).IsRequired().HasMaxLength(300);
        builder.Property(v => v.NationalIdBackUrl).HasMaxLength(300);
        builder.Property(v => v.ExtractedName).HasMaxLength(150);
        builder.Property(v => v.ExtractedNationalId).HasMaxLength(20);
        builder.Property(v => v.RejectionReason).HasMaxLength(300);
        builder.Property(v => v.Status).HasConversion<byte>();

        builder.HasIndex(v => v.Status);

        builder.HasOne(v => v.ReviewedByAdmin)
            .WithMany(a => a.ReviewedVerifications)
            .HasForeignKey(v => v.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}