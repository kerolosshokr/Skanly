using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;


namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class ContractConfiguration : IEntityTypeConfiguration<Contract>
    {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            builder.ToTable("Contracts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FileUrl)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.HasOne(x => x.Booking)
                   .WithOne(x => x.Contract)
                   .HasForeignKey<Contract>(x => x.BookingId);
        }
    }
}
