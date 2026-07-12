// Skanly.Application/Features/Bookings/DTOs/BookingStatsDto.cs
namespace Skanly.Application.Features.Bookings.DTOs;

/// <summary>Used by Admin dashboard analytics.</summary>
public class BookingStatsDto
{
    public int TotalBookings { get; init; }
    public int PendingBookings { get; init; }
    public int ConfirmedBookings { get; init; }
    public int CancelledBookings { get; init; }
    public int RejectedBookings { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalCommission { get; init; }

    public IReadOnlyList<MonthlyBookingPoint> MonthlyTrend { get; init; }
        = new List<MonthlyBookingPoint>();
}

public class MonthlyBookingPoint
{
    public string MonthLabel { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Revenue { get; init; }
}