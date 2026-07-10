// Skanly.Infrastructure/Persistence/Configurations/OwnerConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;
using Skanly.Infrastructure.Identity;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("Owners");
        builder.HasKey(o => o.UserId);
        builder.Property(o => o.UserId).HasMaxLength(450);

        builder.Property(o => o.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(o => o.LastName).IsRequired().HasMaxLength(100);
        builder.Property(o => o.NationalId).HasMaxLength(20);
        builder.Property(o => o.BusinessName).HasMaxLength(150);
        builder.Property(o => o.ProfileImageUrl).HasMaxLength(300);
        builder.Property(o => o.BankAccountInfo).HasMaxLength(300);

        builder.HasIndex(o => o.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL");

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Owner>(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Properties)
            .WithOne(p => p.Owner)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Conversations)
            .WithOne(c => c.Owner)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(o => o.FullName);
    }
}