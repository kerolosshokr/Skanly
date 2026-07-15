// Skanly.Application/Features/Analytics/DTOs/PropertyAnalyticsDto.cs
namespace Skanly.Application.Features.Analytics.DTOs;

public class PropertyAnalyticsDto
{
    // ── KPIs ──────────────────────────────────────────────────────────────────
    public int TotalProperties { get; init; }
    public int ApprovedProperties { get; init; }
    public int PendingApproval { get; init; }
    public int UnavailableProperties { get; init; }
    public int DeletedProperties { get; init; }
    public double ApprovalRate =>
        TotalProperties == 0 ? 0
        : Math.Round((double)ApprovedProperties / TotalProperties * 100, 1);

    public double AveragePlatformRating { get; init; }
    public int NewPropertiesInRange { get; init; }

    // ── Type breakdown ─────────────────────────────────────────────────────────
    public IReadOnlyList<PropertyTypeRow> ByType { get; init; }
        = new List<PropertyTypeRow>();

    // ── Area breakdown ─────────────────────────────────────────────────────────
    public IReadOnlyList<PropertyAreaRow> ByArea { get; init; }
        = new List<PropertyAreaRow>();

    // ── Charts ────────────────────────────────────────────────────────────────
    public ChartDataDto ListingTrendChart { get; init; } = new();
    public PieChartDto PropertyTypeChart { get; init; } = new();
    public PieChartDto ApprovalStatusChart { get; init; } = new();
    public ChartDataDto AvgPriceByAreaChart { get; init; } = new();
    public ChartDataDto OccupancyChart { get; init; } = new();

    // ── Top rated properties ───────────────────────────────────────────────────
    public IReadOnlyList<TopRatedPropertyRow> TopRatedProperties { get; init; }
        = new List<TopRatedPropertyRow>();

    // ── Pending approval queue ─────────────────────────────────────────────────
    public IReadOnlyList<PendingPropertyRow> PendingApprovalQueue { get; init; }
        = new List<PendingPropertyRow>();

    public DateRangeDto DateRange { get; init; } = DateRangeDto.Last30Days();
}

public class PropertyTypeRow
{
    public string TypeDisplay { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
    public decimal AvgPrice { get; init; }
    public double AvgRating { get; init; }
}

public class PropertyAreaRow
{
    public string AreaNameEn { get; init; } = string.Empty;
    public int PropertyCount { get; init; }
    public int BookingCount { get; init; }
    public decimal AvgPrice { get; init; }
    public double OccupancyRate { get; init; }
}

public class TopRatedPropertyRow
{
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public decimal PricePerMonth { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TotalBookings { get; init; }
}

public class PendingPropertyRow
{
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public string TypeDisplay { get; init; } = string.Empty;
    public decimal PricePerMonth { get; init; }
    public DateTime SubmittedAt { get; init; }
    public string WaitingTime { get; init; } = string.Empty;
}