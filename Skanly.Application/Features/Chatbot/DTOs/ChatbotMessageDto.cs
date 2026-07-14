// Skanly.Application/Features/Chatbot/DTOs/ChatbotMessageDto.cs
namespace Skanly.Application.Features.Chatbot.DTOs;

public class ChatbotMessageDto
{
    public long MessageId { get; init; }
    public int ConversationId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsUser => Role == "user";
    public bool IsInstantAnswer { get; init; }
    public string? DetectedIntent { get; init; }
    public DateTime SentAt { get; init; }
    public string TimeLabel => SentAt.ToLocalTime().ToString("hh:mm tt");
}