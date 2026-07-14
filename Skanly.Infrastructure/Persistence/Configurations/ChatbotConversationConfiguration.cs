using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

public class ChatbotConversationConfiguration
    : IEntityTypeConfiguration<ChatbotConversation>
{
    public void Configure(EntityTypeBuilder<ChatbotConversation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.ConversationTitle)
            .HasMaxLength(200);

        builder.HasOne(x => x.RelatedProperty)
            .WithMany()
            .HasForeignKey(x => x.RelatedPropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Messages)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.LastMessageAt
        });
    }
}