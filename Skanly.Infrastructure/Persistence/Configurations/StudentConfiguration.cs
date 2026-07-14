using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;
using Skanly.Infrastructure.Identity;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.UserId);

        builder.Property(s => s.UserId)
               .HasMaxLength(450);

        builder.Property(s => s.FirstName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(s => s.LastName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(s => s.NationalId)
               .HasMaxLength(20);

        builder.Property(s => s.ProfileImageUrl)
               .HasMaxLength(300);

        builder.Property(s => s.Gender)
               .HasConversion<byte>();

        builder.HasIndex(s => s.NationalId)
               .IsUnique()
               .HasFilter("[NationalId] IS NOT NULL");

        // 1:1 with ApplicationUser
        builder.HasOne<ApplicationUser>()
               .WithOne()
               .HasForeignKey<Student>(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Bookings)
               .WithOne(b => b.Student)
               .HasForeignKey(b => b.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        // Favorite relationship is configured in FavoriteConfiguration

        builder.HasMany(s => s.Reviews)
               .WithOne(r => r.Student)
               .HasForeignKey(r => r.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Conversations)
               .WithOne(c => c.Student)
               .HasForeignKey(c => c.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(s => s.FullName);

        builder.Property(s => s.PhoneNumber)
       .HasMaxLength(20);
    }
}