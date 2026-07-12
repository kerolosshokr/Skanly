// Skanly.Application/Features/Bookings/DTOs/BookingDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Bookings.DTOs;

public class BookingDto
{
    public int BookingId { get; init; }

    // Student
    public string StudentId { get; init; } = string.Empty;
    public string StudentFullName { get; init; } = string.Empty;
    public string? StudentImageUrl { get; init; }
    public bool StudentIsVerified { get; init; }

    // Property
    public int PropertyId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public string? PropertyImageUrl { get; init; }
    public string AreaNameEn { get; init; } = string.Empty;
    public string? UniversityNameEn { get; init; }
    public string PropertyTypeDisplay { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;

    // Booking details
    public BookingStatus Status { get; init; }
    public DateOnly CheckInDate { get; init; }
    public DateOnly? CheckOutDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal CommissionRate { get; init; }
    public decimal? CommissionAmount { get; init; }

    // Timestamps
    public DateTime RequestedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public DateTime CreatedAt { get; init; }

    // Flags
    public bool HasContract { get; init; }
    public bool HasReview { get; init; }
    public bool HasSuccessfulPayment { get; init; }

    // Computed helpers
    public string StatusDisplay => Status switch
    {
        BookingStatus.Pending => "Pending",
        BookingStatus.Accepted => "Accepted",
        BookingStatus.Rejected => "Rejected",
        BookingStatus.PaymentPending => "Payment Pending",
        BookingStatus.Confirmed => "Confirmed",
        BookingStatus.Cancelled => "Cancelled",
        _ => Status.ToString()
    };

    public string StatusBadgeClass => Status switch
    {
        BookingStatus.Pending => "bg-warning text-dark",
        BookingStatus.Accepted => "bg-primary",
        BookingStatus.PaymentPending => "bg-info",
        BookingStatus.Confirmed => "bg-success",
        BookingStatus.Rejected => "bg-danger",
        BookingStatus.Cancelled => "bg-secondary",
        _ => "bg-secondary"
    };

    public string StatusIcon => Status switch
    {
        BookingStatus.Pending => "fa-clock",
        BookingStatus.Accepted => "fa-check",
        BookingStatus.PaymentPending => "fa-credit-card",
        BookingStatus.Confirmed => "fa-check-double",
        BookingStatus.Rejected => "fa-times",
        BookingStatus.Cancelled => "fa-ban",
        _ => "fa-calendar"
    };

    public bool CanCancel =>
        Status == BookingStatus.Pending ||
        Status == BookingStatus.Accepted;

    public bool CanPay =>
        Status == BookingStatus.Accepted ||
        Status == BookingStatus.PaymentPending;

    public bool CanReview =>
        Status == BookingStatus.Confirmed && !HasReview;

    public string TimeAgo => GetTimeAgo(RequestedAt);

    private static string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        return span.TotalMinutes < 1 ? "Just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays}d ago"
            : dt.ToString("MMM dd, yyyy");
    }
}