// Skanly.Domain/Entities/ChatbotMessage.cs
namespace Skanly.Domain.Entities;

public class ChatbotMessage
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public ChatbotConversation Conversation { get; set; } = null!;

    /// <summary>"user" | "assistant"</summary>
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Intent detected before Claude was called.
    /// null if message went straight to Claude.
    /// </summary>
    public string? DetectedIntent { get; set; }

    /// <summary>True if answered from FAQ/DB without calling Claude.</summary>
    public bool IsInstantAnswer { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int? TokensUsed { get; set; }
}