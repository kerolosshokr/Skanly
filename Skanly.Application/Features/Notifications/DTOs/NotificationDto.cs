// Skanly.Application/Features/Notifications/DTOs/NotificationDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Notifications.DTOs;

public class NotificationDto
{
    public long NotificationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public bool IsRead { get; init; }
    public int? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TimeAgo => GetTimeAgo(CreatedAt);
    public string IconClass => Type switch
    {
        NotificationType.BookingUpdate => "fa-calendar-check text-primary",
        NotificationType.NewMessage => "fa-envelope text-info",
        NotificationType.PropertyApproval => "fa-home text-success",
        NotificationType.VerificationApproval => "fa-shield-check text-warning",
        NotificationType.PaymentConfirmation => "fa-credit-card text-success",
        _ => "fa-bell text-secondary"
    };

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        return span.TotalMinutes < 1 ? "Just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays}d ago"
            : dateTime.ToString("MMM dd");
    }
}