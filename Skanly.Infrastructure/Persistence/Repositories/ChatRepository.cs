using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class ChatRepository : GenericRepository<ChatConversation>, IChatRepository
{
    public ChatRepository(SkanlyDbContext context) : base(context) { }

    public async Task<ChatConversation> GetOrCreateConversationAsync(
        string studentId,
        string ownerId,
        int? propertyId,
        CancellationToken ct = default)
    {
        var existing = await _dbSet
            .FirstOrDefaultAsync(c =>
                c.StudentId == studentId &&
                c.OwnerId == ownerId &&
                c.PropertyId == propertyId, ct);

        if (existing is not null) return existing;

        var conversation = new ChatConversation
        {
            StudentId = studentId,
            OwnerId = ownerId,
            PropertyId = propertyId
        };

        await _dbSet.AddAsync(conversation, ct);
        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task<IReadOnlyList<ChatConversation>> GetConversationsForUserAsync(
        string userId,
        CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.StudentId == userId || c.OwnerId == userId)
            .Include(c => c.Student)
            .Include(c => c.Owner)
            .Include(c => c.Property).ThenInclude(p => p!.Images.Where(i => i.IsPrimary))
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesAsync(
        int conversationId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(m => m.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task MarkMessagesAsReadAsync(
        int conversationId,
        string readByUserId,
        CancellationToken ct = default)
    {
        await _context.ChatMessages
            .Where(m =>
                m.ConversationId == conversationId &&
                m.SenderId != readByUserId &&
                !m.IsRead)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(m => m.IsRead, true), ct);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        => await _context.ChatMessages
            .CountAsync(m =>
                m.Conversation.StudentId == userId ||
                m.Conversation.OwnerId == userId &&
                m.SenderId != userId &&
                !m.IsRead, ct);
}