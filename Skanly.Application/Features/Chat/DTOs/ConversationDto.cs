// Skanly.Application/Features/Chat/DTOs/ConversationDto.cs
namespace Skanly.Application.Features.Chat.DTOs;

public class ConversationDto
{
    public int ConversationId { get; init; }
    public string StudentId { get; init; } = string.Empty;
    public string StudentFullName { get; init; } = string.Empty;
    public string? StudentImageUrl { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;
    public string? OwnerImageUrl { get; init; }
    public int? PropertyId { get; init; }
    public string? PropertyTitle { get; init; }
    public string? PropertyImageUrl { get; init; }

    // Last message preview
    public string? LastMessageText { get; init; }
    public string? LastMessageImageUrl { get; init; }
    public bool LastMessageIsImage => string.IsNullOrEmpty(LastMessageText)
                                   && LastMessageImageUrl != null;
    public string LastMessagePreview => LastMessageIsImage
        ? "📷 Photo"
        : (LastMessageText?.Length > 60
            ? LastMessageText[..60] + "…"
            : LastMessageText ?? "");
    public DateTime? LastMessageAt { get; init; }
    public string LastMessageTimeAgo => GetTimeAgo(LastMessageAt);

    // Context for the current viewer
    public string OtherPartyId { get; init; } = string.Empty;
    public string OtherPartyFullName { get; init; } = string.Empty;
    public string? OtherPartyImageUrl { get; init; }
    public int UnreadCount { get; init; }
    public bool IsOnline { get; init; }

    private static string GetTimeAgo(DateTime? dt)
    {
        if (dt is null) return string.Empty;
        var span = DateTime.UtcNow - dt.Value;
        return span.TotalMinutes < 1 ? "Just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays}d"
            : dt.Value.ToString("MMM dd");
    }
}