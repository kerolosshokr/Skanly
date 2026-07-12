// Skanly.Application/Features/Owners/DTOs/BookingRequestDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Owners.DTOs;

public class BookingRequestDto
{
    public int BookingId { get; init; }
    public string StudentFullName { get; init; } = string.Empty;
    public string? StudentImageUrl { get; init; }
    public string StudentEmail { get; init; } = string.Empty;
    public bool StudentIsVerified { get; init; }
    public int PropertyId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public string? PropertyImageUrl { get; init; }
    public string AreaNameEn { get; init; } = string.Empty;
    public DateOnly CheckInDate { get; init; }
    public DateOnly? CheckOutDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public BookingStatus Status { get; init; }
    public string StatusDisplay => Status.ToString();
    public string StatusBadgeClass => Status switch
    {
        BookingStatus.Pending => "bg-warning text-dark",
        BookingStatus.Accepted => "bg-primary",
        BookingStatus.Confirmed => "bg-success",
        BookingStatus.Rejected => "bg-danger",
        BookingStatus.Cancelled => "bg-secondary",
        BookingStatus.PaymentPending => "bg-info",
        _ => "bg-secondary"
    };
    public DateTime RequestedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string TimeAgo => GetTimeAgo(RequestedAt);

    private static string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        return span.TotalMinutes < 1 ? "Just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays}d ago"
            : dt.ToString("MMM dd");
    }
}