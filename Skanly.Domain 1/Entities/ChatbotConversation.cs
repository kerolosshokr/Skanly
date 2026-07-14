// Skanly.Domain/Entities/ChatbotConversation.cs
namespace Skanly.Domain.Entities;

public class ChatbotConversation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Optional context — what page the student was on
    public int? RelatedPropertyId { get; set; }
    public Property? RelatedProperty { get; set; }

    public string? ConversationTitle { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatbotMessage> Messages { get; set; }
        = new List<ChatbotMessage>();
}