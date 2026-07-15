// Skanly.Application/Features/Analytics/DTOs/ChartDataDto.cs
namespace Skanly.Application.Features.Analytics.DTOs;

/// <summary>
/// Generic chart data structure consumed by Chart.js on the frontend.
/// Supports single and multi-dataset charts.
/// </summary>
public class ChartDataDto
{
    public IReadOnlyList<string> Labels { get; init; }
        = new List<string>();

    public IReadOnlyList<ChartDatasetDto> Datasets { get; init; }
        = new List<ChartDatasetDto>();
}

public class ChartDatasetDto
{
    public string Label { get; init; } = string.Empty;
    public IReadOnlyList<double> Data { get; init; }
        = new List<double>();
    public string? BackgroundColor { get; init; }
    public string? BorderColor { get; init; }
    public string? Type { get; init; }  // "bar" | "line" | "pie"
    public bool Fill { get; init; }
    public int BorderWidth { get; init; } = 2;
}

/// <summary>
/// Pie / Doughnut chart — label → value pairs.
/// </summary>
public class PieChartDto
{
    public IReadOnlyList<string> Labels { get; init; }
        = new List<string>();

    public IReadOnlyList<double> Data { get; init; }
        = new List<double>();

    public IReadOnlyList<string> BackgroundColors { get; init; }
        = new List<string>();
}

/// <summary>Single data point on a time series.</summary>
public record TimeSeriesPoint(string Label, double Value);

/// <summary>KPI card data.</summary>
public class KpiDto
{
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? SubValue { get; init; }
    public string? TrendPercent { get; init; }
    public bool TrendUp { get; init; }
    public string Icon { get; init; } = "fa-chart-line";
    public string Color { get; init; } = "primary";
}