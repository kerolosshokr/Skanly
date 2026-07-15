// Skanly.Application/Features/Analytics/DTOs/BookingAnalyticsDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Analytics.DTOs;

public class BookingAnalyticsDto
{
    // ── KPIs ──────────────────────────────────────────────────────────────────
    public int TotalBookings { get; init; }
    public int PendingBookings { get; init; }
    public int AcceptedBookings { get; init; }
    public int ConfirmedBookings { get; init; }
    public int CancelledBookings { get; init; }
    public int RejectedBookings { get; init; }
    public double ConversionRate { get; init; }
    public double AcceptanceRate { get; init; }
    public double CancellationRate { get; init; }
    public double AvgResponseHours { get; init; }

    // ── Charts ────────────────────────────────────────────────────────────────
    public ChartDataDto BookingTrendChart { get; init; } = new();
    public PieChartDto BookingStatusChart { get; init; } = new();
    public ChartDataDto DailyBookingsChart { get; init; } = new();
    public PieChartDto PropertyTypeBookingsChart { get; init; } = new();

    // ── Top booked properties ──────────────────────────────────────────────────
    public IReadOnlyList<TopBookedPropertyRow> TopBookedProperties { get; init; }
        = new List<TopBookedPropertyRow>();

    // ── Top booking areas ──────────────────────────────────────────────────────
    public IReadOnlyList<AreaBookingRow> TopBookingAreas { get; init; }
        = new List<AreaBookingRow>();

    // ── Recent bookings list ───────────────────────────────────────────────────
    public IReadOnlyList<RecentBookingRow> RecentBookings { get; init; }
        = new List<RecentBookingRow>();

    public DateRangeDto DateRange { get; init; } = DateRangeDto.Last30Days();
}

public class TopBookedPropertyRow
{
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public int TotalBookings { get; init; }
    public int ConfirmedBookings { get; init; }
    public decimal TotalRevenue { get; init; }
}

public class AreaBookingRow
{
    public string AreaNameEn { get; init; } = string.Empty;
    public int BookingCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public double Percentage { get; init; }
}

public class RecentBookingRow
{
    public int BookingId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string PropertyTitle { get; init; } = string.Empty;
    public BookingStatus Status { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public string StatusBadge { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime RequestedAt { get; init; }
}