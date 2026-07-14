using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Notifications.DTOs;

public class NotificationDto
{
    public long NotificationId { get; init; }
    public string UserId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public NotificationType Type { get; init; }
    public bool IsRead { get; init; }

    public int? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }

    public DateTime CreatedAt { get; init; }

    // Computed Properties
    public string TimeAgo => GetTimeAgo(CreatedAt);

    public string DateGroup => GetDateGroup(CreatedAt);

    // لو عندك Views القديمة بتستخدم IconClass هيفضل شغال
    public string IconClass => $"{IconClassName} {IconColorClass}";

    // اسم الأيقونة فقط
    public string IconClassName => Type switch
    {
        NotificationType.BookingUpdate => "fa-calendar-check",
        NotificationType.NewMessage => "fa-envelope",
        NotificationType.PropertyApproval => "fa-home",
        NotificationType.VerificationApproval => "fa-shield-check",
        NotificationType.PaymentConfirmation => "fa-credit-card",
        _ => "fa-bell"
    };

    // لون الأيقونة
    public string IconColorClass => Type switch
    {
        NotificationType.BookingUpdate => "text-primary",
        NotificationType.NewMessage => "text-info",
        NotificationType.PropertyApproval => "text-success",
        NotificationType.VerificationApproval => "text-warning",
        NotificationType.PaymentConfirmation => "text-success",
        _ => "text-secondary"
    };

    // لون الخلفية
    public string IconBgClass => Type switch
    {
        NotificationType.BookingUpdate => "bg-primary",
        NotificationType.NewMessage => "bg-info",
        NotificationType.PropertyApproval => "bg-success",
        NotificationType.VerificationApproval => "bg-warning",
        NotificationType.PaymentConfirmation => "bg-success",
        _ => "bg-secondary"
    };

    /// <summary>
    /// URL to navigate when the notification is clicked.
    /// </summary>
    public string? NavigationUrl => (RelatedEntityType, Type) switch
    {
        ("Booking", NotificationType.BookingUpdate)
            => $"/Student/Bookings/Details/{RelatedEntityId}",

        ("Booking", NotificationType.PaymentConfirmation)
            => $"/Student/Bookings/Details/{RelatedEntityId}",

        ("Conversation", NotificationType.NewMessage)
            => $"/Chat/StudentIndex?conversationId={RelatedEntityId}",

        ("Property", NotificationType.PropertyApproval)
            => "/Owner/Properties/Index",

        ("Verification", NotificationType.VerificationApproval)
            => "/Student/Profile/VerifyIdentity",

        ("Report", _)
            => "/Reports/MyReports",

        _ => null
    };

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;

        return span.TotalSeconds < 60 ? "Just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays}d ago"
            : span.TotalDays < 30 ? $"{(int)span.TotalDays} days ago"
            : dateTime.ToString("MMM dd, yyyy");
    }

    private static string GetDateGroup(DateTime dateTime)
    {
        if (dateTime.Date == DateTime.Today)
            return "Today";

        if (dateTime.Date == DateTime.Today.AddDays(-1))
            return "Yesterday";

        if (dateTime.Date >= DateTime.Today.AddDays(-7))
            return "This Week";

        if (dateTime.Date >= DateTime.Today.AddDays(-30))
            return "This Month";

        return dateTime.ToString("MMMM yyyy");
    }
}