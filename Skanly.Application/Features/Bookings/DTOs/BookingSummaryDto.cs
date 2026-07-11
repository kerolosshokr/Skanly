// Skanly.Application/Features/Bookings/DTOs/BookingSummaryDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Bookings.DTOs;

public class BookingSummaryDto
{
    public int BookingId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public string? PropertyImageUrl { get; init; }
    public string AreaNameEn { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public BookingStatus Status { get; init; }
    public string StatusDisplay => Status.ToString();
    public string StatusBadgeClass => Status switch
    {
        BookingStatus.Confirmed => "bg-success",
        BookingStatus.Accepted => "bg-primary",
        BookingStatus.Pending => "bg-warning text-dark",
        BookingStatus.PaymentPending => "bg-info",
        BookingStatus.Rejected => "bg-danger",
        BookingStatus.Cancelled => "bg-secondary",
        _ => "bg-secondary"
    };
    public DateOnly CheckInDate { get; init; }
    public DateTime RequestedAt { get; init; }
}