using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;


namespace Skanly.Infrastructure.Persistence.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Message)
                   .IsRequired()
                   .HasMaxLength(2000);

            builder.Property(x => x.SenderId)
                   .IsRequired();

            builder.Property(x => x.IsRead)
                   .HasDefaultValue(false);

            builder.Property(x => x.SentAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Conversation)
                   .WithMany(x => x.Messages)
                   .HasForeignKey(x => x.ConversationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
