using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");

        builder.HasKey(f => f.Id);

        builder.HasIndex(f => new { f.StudentId, f.PropertyId })
               .IsUnique();

        builder.HasIndex(f => f.StudentId);

        builder.HasOne(f => f.Student)
               .WithMany(s => s.Favorites)
               .HasForeignKey(f => f.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Property)
               .WithMany(p => p.Favorites)
               .HasForeignKey(f => f.PropertyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}