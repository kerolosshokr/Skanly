// Skanly.Application/Features/Chatbot/DTOs/SendChatbotMessageDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Chatbot.DTOs;

public class SendChatbotMessageDto
{
    /// <summary>
    /// null = start new conversation.
    /// </summary>
    public int? ConversationId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Property the student is currently viewing (optional context).
    /// </summary>
    public int? CurrentPropertyId { get; set; }

    /// <summary>Stream the response via SSE.</summary>
    public bool Stream { get; set; } = true;
}