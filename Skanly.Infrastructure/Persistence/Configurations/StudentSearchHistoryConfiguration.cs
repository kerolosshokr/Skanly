using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class StudentSearchHistoryConfiguration : IEntityTypeConfiguration<StudentSearchHistory>

{
    public void Configure(EntityTypeBuilder<StudentSearchHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StudentId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.MinPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.MaxPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.PropertyType)
            .HasConversion<byte>();

        builder.Property(x => x.SearchedAt)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.StudentId,
            x.SearchedAt
        });
    }
}