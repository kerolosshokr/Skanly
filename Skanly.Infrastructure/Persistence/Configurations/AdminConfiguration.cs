using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations
{
    public  class AdminConfiguration : IEntityTypeConfiguration<Admin>
    {

        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            builder.ToTable("Admins");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                   .IsRequired();

            builder.Property(x => x.FullName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.PhoneNumber)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.HasIndex(x => x.UserId)
                   .IsUnique();
        }

    }
}
