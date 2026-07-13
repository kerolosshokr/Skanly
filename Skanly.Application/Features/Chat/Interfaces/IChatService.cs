// Skanly.Application/Features/Chat/Interfaces/IChatService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Chat.DTOs;

namespace Skanly.Application.Features.Chat.Interfaces;

public interface IChatService
{
    // ── Conversations ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or creates a conversation between a student and owner.
    /// Safe to call multiple times — always returns the same conversation.
    /// </summary>
    Task<ServiceResult<ConversationDto>> GetOrCreateConversationAsync(
        string requesterId,
        StartConversationDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all conversations for the calling user (student or owner),
    /// ordered by most recently active.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<ConversationDto>>> GetConversationsAsync(
        string userId,
        CancellationToken ct = default);

    Task<ServiceResult<ConversationDto>> GetConversationByIdAsync(
        string userId,
        int conversationId,
        CancellationToken ct = default);

    // ── Messages ──────────────────────────────────────────────────────────────

    Task<ServiceResult<PagedResult<MessageDto>>> GetMessagesAsync(
        string userId,
        int conversationId,
        int pageNumber = 1,
        int pageSize = 40,
        CancellationToken ct = default);

    /// <summary>
    /// Persists a message. Called by the Hub after connection validation.
    /// </summary>
    Task<ServiceResult<MessageDto>> SendMessageAsync(
        string senderId,
        SendMessageDto dto,
        CancellationToken ct = default);

    // ── Read Receipts ─────────────────────────────────────────────────────────

    /// <summary>
    /// Marks all unread messages in a conversation as read for the caller.
    /// Returns the IDs of messages that were marked (for hub relay).
    /// </summary>
    Task<ServiceResult<IReadOnlyList<long>>> MarkMessagesReadAsync(
        string userId,
        int conversationId,
        CancellationToken ct = default);

    // ── Unread counts ─────────────────────────────────────────────────────────

    Task<ServiceResult<int>> GetTotalUnreadCountAsync(
        string userId,
        CancellationToken ct = default);

    Task<ServiceResult<int>> GetConversationUnreadCountAsync(
        string userId,
        int conversationId,
        CancellationToken ct = default);
}