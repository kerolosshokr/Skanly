namespace Skanly.Application.Features.Owners.DTOs;

public class EarningsSummaryDto
{
    public decimal TotalEarnings { get; init; }
    public decimal MonthlyEarnings { get; init; }
    public decimal TotalCommissionPaid { get; init; }
    public int TotalConfirmedBookings { get; init; }

    public IReadOnlyList<PropertyEarningsRow> ByProperty { get; init; }
        = new List<PropertyEarningsRow>();

    public IReadOnlyList<MonthlyEarningsPoint> MonthlyBreakdown { get; init; }
        = new List<MonthlyEarningsPoint>();
}

public class PropertyEarningsRow
{
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public int ConfirmedBookings { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal CommissionPaid { get; init; }
    public decimal NetEarnings { get; init; }
}
