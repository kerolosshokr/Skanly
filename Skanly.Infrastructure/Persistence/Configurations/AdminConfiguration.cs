// Skanly.Infrastructure/Persistence/Configurations/AdminConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;
using Skanly.Infrastructure.Identity;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");
        builder.HasKey(a => a.UserId);
        builder.Property(a => a.UserId).HasMaxLength(450);

        builder.Property(a => a.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.LastName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Department).HasMaxLength(100);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.ReviewedVerifications)
            .WithOne(v => v.ReviewedByAdmin)
            .HasForeignKey(v => v.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.ResolvedReports)
            .WithOne(r => r.ResolvedByAdmin)
            .HasForeignKey(r => r.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.CommissionSettings)
            .WithOne(c => c.SetByAdmin)
            .HasForeignKey(c => c.SetByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(a => a.FullName);
    }
}