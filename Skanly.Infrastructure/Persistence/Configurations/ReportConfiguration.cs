// Skanly.Infrastructure/Persistence/Configurations/ReportConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReporterId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.ReportedUserId).HasMaxLength(450);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.Resolution).HasMaxLength(1000);
        builder.Property(r => r.ReportType).HasConversion<byte>();
        builder.Property(r => r.Status).HasConversion<byte>();

        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.ReportedProperty)
            .WithMany(p => p.Reports)
            .HasForeignKey(r => r.ReportedPropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ResolvedByAdmin)
            .WithMany(a => a.ResolvedReports)
            .HasForeignKey(r => r.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Report_Target",
            "[ReportedPropertyId] IS NOT NULL OR [ReportedUserId] IS NOT NULL"));
    }
}