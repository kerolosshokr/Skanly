// Skanly.Application/Features/Chatbot/DTOs/ChatbotConversationDto.cs
namespace Skanly.Application.Features.Chatbot.DTOs;

public class ChatbotConversationDto
{
    public int ConversationId { get; init; }
    public string? ConversationTitle { get; init; }
    public int? RelatedPropertyId { get; init; }
    public string? RelatedPropertyTitle { get; init; }
    public IReadOnlyList<ChatbotMessageDto> Messages { get; init; }
        = new List<ChatbotMessageDto>();
    public DateTime LastMessageAt { get; init; }
    public string LastMessagePreview { get; init; } = string.Empty;
}