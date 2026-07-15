// Skanly.Application/Features/Analytics/DTOs/DateRangeDto.cs
namespace Skanly.Application.Features.Analytics.DTOs;

public class DateRangeDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    // ── Preset factory methods ─────────────────────────────────────────────────

    public static DateRangeDto Last7Days()
        => new()
        {
            From = DateTime.UtcNow.AddDays(-7).Date,
            To = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1)
        };

    public static DateRangeDto Last30Days()
        => new()
        {
            From = DateTime.UtcNow.AddDays(-30).Date,
            To = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1)
        };

    public static DateRangeDto Last90Days()
        => new()
        {
            From = DateTime.UtcNow.AddDays(-90).Date,
            To = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1)
        };

    public static DateRangeDto ThisMonth()
    {
        var now = DateTime.UtcNow;
        return new()
        {
            From = new DateTime(now.Year, now.Month, 1),
            To = new DateTime(now.Year, now.Month, 1)
                       .AddMonths(1).AddTicks(-1)
        };
    }

    public static DateRangeDto ThisYear()
    {
        var year = DateTime.UtcNow.Year;
        return new()
        {
            From = new DateTime(year, 1, 1),
            To = new DateTime(year + 1, 1, 1).AddTicks(-1)
        };
    }

    public static DateRangeDto LastYear()
    {
        var year = DateTime.UtcNow.Year - 1;
        return new()
        {
            From = new DateTime(year, 1, 1),
            To = new DateTime(year + 1, 1, 1).AddTicks(-1)
        };
    }

    public string Label =>
        (To - From).TotalDays switch
        {
            <= 7 => "Last 7 Days",
            <= 31 => "Last 30 Days",
            <= 92 => "Last 90 Days",
            <= 366 => "This Year",
            _ => $"{From:MMM dd} – {To:MMM dd, yyyy}"
        };

    public int TotalDays => (int)(To - From).TotalDays;
}