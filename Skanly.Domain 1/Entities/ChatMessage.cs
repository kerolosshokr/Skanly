// Skanly.Domain/Entities/ChatMessage.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class ChatMessage : IAggregateRoot
{
    public long MessageId { get; set; }

    [Required]
    public int ConversationId { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? MessageText { get; set; }

    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ChatConversation Conversation { get; set; } = null!;
}