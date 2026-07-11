using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IChatRepository : IRepository<ChatConversation>
{
    Task<ChatConversation> GetOrCreateConversationAsync(
        string studentId,
        string ownerId,
        int? propertyId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ChatConversation>> GetConversationsForUserAsync(
        string userId,
        CancellationToken ct = default);

    Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesAsync(
        int conversationId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    Task MarkMessagesAsReadAsync(
        int conversationId,
        string readByUserId,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
}