// Skanly.Domain/Entities/ChatConversation.cs
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class ChatConversation : BaseEntity<int>, IAggregateRoot
{
    public string StudentId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public int? PropertyId { get; set; }

    public DateTime? LastMessageAt { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
    public Owner Owner { get; set; } = null!;
    public Property? Property { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}