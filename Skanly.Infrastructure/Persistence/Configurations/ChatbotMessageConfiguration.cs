using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

public class ChatbotMessageConfiguration
    : IEntityTypeConfiguration<ChatbotMessage>
{
    public void Configure(EntityTypeBuilder<ChatbotMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.DetectedIntent)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.ConversationId,
            x.SentAt
        });
    }
}