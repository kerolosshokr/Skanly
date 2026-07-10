// Skanly.Infrastructure/Persistence/Configurations/ChatMessageConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageText).HasMaxLength(1000);
        builder.Property(m => m.ImageUrl).HasMaxLength(300);
        builder.Property(m => m.SenderId).IsRequired().HasMaxLength(450);

        builder.HasIndex(m => new { m.ConversationId, m.SentAt })
            .HasDatabaseName("IX_Message_ConversationId");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Message_Content",
            "[MessageText] IS NOT NULL OR [ImageUrl] IS NOT NULL"));
    }
}