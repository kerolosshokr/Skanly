// Skanly.Infrastructure/Persistence/Configurations/CommissionSettingConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class CommissionSettingConfiguration : IEntityTypeConfiguration<CommissionSetting>
{
    public void Configure(EntityTypeBuilder<CommissionSetting> builder)
    {
        builder.ToTable("CommissionSettings");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Rate).HasColumnType("decimal(5,2)");
        builder.Property(c => c.SetByAdminId).IsRequired().HasMaxLength(450);
    }
}