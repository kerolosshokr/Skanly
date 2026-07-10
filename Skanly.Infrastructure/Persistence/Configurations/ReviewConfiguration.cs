using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Rating)
                   .IsRequired();

            builder.Property(x => x.Comment)
                   .HasMaxLength(1000);

            builder.HasOne(x => x.Booking)
                   .WithOne(x => x.Review)
                   .HasForeignKey<Review>(x => x.BookingId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Student)
                   .WithMany(x => x.Reviews)
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Property)
                   .WithMany(x => x.Reviews)
                   .HasForeignKey(x => x.PropertyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}