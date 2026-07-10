// Skanly.Infrastructure/Persistence/Configurations/ContractConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contracts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContractNumber).IsRequired().HasMaxLength(50);
        builder.Property(c => c.PdfUrl).IsRequired().HasMaxLength(300);

        builder.HasIndex(c => c.BookingId).IsUnique();
        builder.HasIndex(c => c.ContractNumber).IsUnique();
    }
}