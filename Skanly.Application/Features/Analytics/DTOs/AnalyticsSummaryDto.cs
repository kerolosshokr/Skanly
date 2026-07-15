// Skanly.Application/Features/Analytics/DTOs/AnalyticsSummaryDto.cs
namespace Skanly.Application.Features.Analytics.DTOs;

/// <summary>
/// Top-level summary for the main Analytics dashboard page.
/// All counts are for the selected date range.
/// </summary>
public class AnalyticsSummaryDto
{
    // ── KPI Counters ──────────────────────────────────────────────────────────
    public int TotalUsers { get; init; }
    public int NewUsersInRange { get; init; }
    public int TotalStudents { get; init; }
    public int TotalOwners { get; init; }
    public int VerifiedStudents { get; init; }
    public double VerificationRate =>
        TotalStudents == 0 ? 0
        : Math.Round((double)VerifiedStudents / TotalStudents * 100, 1);

    public int TotalProperties { get; init; }
    public int ApprovedProperties { get; init; }
    public int PendingProperties { get; init; }

    public int TotalBookings { get; init; }
    public int ConfirmedBookings { get; init; }
    public int PendingBookings { get; init; }
    public double BookingConversionRate =>
        TotalBookings == 0 ? 0
        : Math.Round((double)ConfirmedBookings / TotalBookings * 100, 1);

    public decimal TotalRevenue { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal AverageBookingValue { get; init; }

    public int TotalReviews { get; init; }
    public double AveragePlatformRating { get; init; }

    public int TotalReports { get; init; }
    public int OpenReports { get; init; }

    // ── Period-over-period comparison ─────────────────────────────────────────
    public double UserGrowthPct { get; init; }
    public double BookingGrowthPct { get; init; }
    public double RevenueGrowthPct { get; init; }

    // ── Chart data ────────────────────────────────────────────────────────────
    public ChartDataDto DailyActivityChart { get; init; } = new();
    public PieChartDto BookingStatusChart { get; init; } = new();
    public PieChartDto UserRoleChart { get; init; } = new();

    // ── Date range ────────────────────────────────────────────────────────────
    public DateRangeDto DateRange { get; init; } = DateRangeDto.Last30Days();
}