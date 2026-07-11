// Skanly.Application/Common/Interfaces/Repositories/IChatRepository.cs
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IChatRepository : IRepository<ChatConversation>
{
    /// <summary>
    /// Gets or creates a conversation between a student and owner
    /// for a specific property. Idempotent — calling twice returns same conversation.
    /// </summary>
    Task<ChatConversation> GetOrCreateConversationAsync(
        string studentId,
        string ownerId,
        int? propertyId,
        CancellationToken ct = default);

    /// <summary>Returns all conversations for a user (student or owner), ordered by LastMessageAt.</summary>
    Task<IReadOnlyList<ChatConversation>> GetConversationsForUserAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>Returns paged messages for a conversation, newest-last.</summary>
    Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesAsync(
        int conversationId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Marks all unread messages in a conversation as read for a user.</summary>
    Task MarkMessagesAsReadAsync(
        int conversationId,
        string readByUserId,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
}