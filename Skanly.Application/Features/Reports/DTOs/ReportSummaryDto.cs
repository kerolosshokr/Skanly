// Skanly.Application/Features/Reports/DTOs/ReportSummaryDto.cs
namespace Skanly.Application.Features.Reports.DTOs;

/// <summary>
/// Aggregated stats for the Admin dashboard reports widget.
/// </summary>
public class ReportSummaryDto
{
    public int TotalOpen { get; init; }
    public int TotalUnderInvestigation { get; init; }
    public int TotalResolved { get; init; }
    public int TotalDismissed { get; init; }
    public int TotalAllTime { get; init; }

    public IReadOnlyList<TypeBreakdown> ByType { get; init; }
        = new List<TypeBreakdown>();
}

public class TypeBreakdown
{
    public string TypeDisplay { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public int Count { get; init; }
}