// Skanly.Application/Features/Chat/DTOs/MessageDto.cs
namespace Skanly.Application.Features.Chat.DTOs;

public class MessageDto
{
    public long MessageId { get; init; }
    public int ConversationId { get; init; }
    public string SenderId { get; init; } = string.Empty;
    public string SenderFullName { get; init; } = string.Empty;
    public string? SenderImageUrl { get; init; }
    public string? MessageText { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsImage => !string.IsNullOrEmpty(ImageUrl);
    public bool IsRead { get; init; }
    public DateTime SentAt { get; init; }
    public string TimeLabel => SentAt.ToLocalTime().ToString("hh:mm tt");
    public string DateLabel => SentAt.Date == DateTime.Today
        ? "Today"
        : SentAt.Date == DateTime.Today.AddDays(-1)
            ? "Yesterday"
            : SentAt.ToString("MMMM dd, yyyy");
}