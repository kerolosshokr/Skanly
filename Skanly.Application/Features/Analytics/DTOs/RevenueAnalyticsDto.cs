// Skanly.Application/Features/Analytics/DTOs/RevenueAnalyticsDto.cs
namespace Skanly.Application.Features.Analytics.DTOs;

public class RevenueAnalyticsDto
{
    // ── KPIs ──────────────────────────────────────────────────────────────────
    public decimal TotalRevenue { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal NetOwnerPayouts { get; init; }
    public decimal AverageBookingValue { get; init; }
    public decimal PreviousPeriodRevenue { get; init; }
    public double RevenueGrowthPct =>
        PreviousPeriodRevenue == 0 ? 0
        : Math.Round((double)(TotalRevenue - PreviousPeriodRevenue) /
                     (double)PreviousPeriodRevenue * 100, 1);

    // ── Payment method breakdown ───────────────────────────────────────────────
    public IReadOnlyList<PaymentMethodRow> ByPaymentMethod { get; init; }
        = new List<PaymentMethodRow>();

    // ── Charts ────────────────────────────────────────────────────────────────
    public ChartDataDto RevenueTrendChart { get; init; } = new();
    public ChartDataDto RevenueVsCommissionChart { get; init; } = new();
    public PieChartDto PaymentMethodChart { get; init; } = new();
    public ChartDataDto MonthlyComparisonChart { get; init; } = new();

    // ── Top revenue owners ─────────────────────────────────────────────────────
    public IReadOnlyList<TopOwnerRevenueRow> TopOwnersByRevenue { get; init; }
        = new List<TopOwnerRevenueRow>();

    // ── Recent transactions ────────────────────────────────────────────────────
    public IReadOnlyList<RecentTransactionRow> RecentTransactions { get; init; }
        = new List<RecentTransactionRow>();

    // ── Monthly breakdown table ────────────────────────────────────────────────
    public IReadOnlyList<MonthlyRevenueRow> MonthlyBreakdown { get; init; }
        = new List<MonthlyRevenueRow>();

    public DateRangeDto DateRange { get; init; } = DateRangeDto.Last30Days();
}

public class PaymentMethodRow
{
    public string Method { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal TotalAmount { get; init; }
    public double Percentage { get; init; }
}

public class TopOwnerRevenueRow
{
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public int PropertyCount { get; init; }
    public int ConfirmedBookings { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal CommissionPaid { get; init; }
    public decimal NetPayout { get; init; }
}

public class RecentTransactionRow
{
    public int PaymentId { get; init; }
    public int BookingId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string PropertyTitle { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? TransactionRef { get; init; }
    public DateTime PaidAt { get; init; }
}

public class MonthlyRevenueRow
{
    public string MonthLabel { get; init; } = string.Empty;
    public int BookingCount { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal Commission { get; init; }
    public decimal NetPayouts { get; init; }
    public double GrowthPct { get; init; }
}