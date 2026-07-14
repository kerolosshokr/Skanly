// Skanly.Application/Features/Chatbot/Interfaces/IChatbotService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Chatbot.DTOs;

namespace Skanly.Application.Features.Chatbot.Interfaces;

public interface IChatbotService
{
    /// <summary>
    /// Sends a message and returns the assistant response.
    /// Creates a new conversation if ConversationId is null.
    /// </summary>
    Task<ServiceResult<ChatbotMessageDto>> SendMessageAsync(
        string userId,
        SendChatbotMessageDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Streams the assistant response token-by-token.
    /// Caller provides callbacks for each token and completion.
    /// Persists the full response after streaming completes.
    /// </summary>
    Task<ServiceResult<int>> StreamMessageAsync(
        string userId,
        SendChatbotMessageDto dto,
        Func<string, Task> onToken,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the full conversation with all messages.
    /// </summary>
    Task<ServiceResult<ChatbotConversationDto>> GetConversationAsync(
        string userId,
        int conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns recent conversations for the user.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<ChatbotConversationDto>>>
        GetRecentConversationsAsync(
            string userId,
            int count = 5,
            CancellationToken ct = default);

    /// <summary>
    /// Starts a new conversation (clears context for a fresh start).
    /// </summary>
    Task<ServiceResult<int>> StartNewConversationAsync(
        string userId,
        int? relatedPropertyId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Builds the context object for a given user and optional property.
    /// </summary>
    Task<ChatbotContextDto> BuildContextAsync(
        string userId,
        int? currentPropertyId = null,
        CancellationToken ct = default);
}