// Skanly.Application/Features/Reviews/DTOs/ReviewDto.cs
namespace Skanly.Application.Features.Reviews.DTOs;

public class ReviewDto
{
    public int ReviewId { get; init; }
    public int BookingId { get; init; }
    public string StudentId { get; init; } = string.Empty;
    public string StudentFullName { get; init; } = string.Empty;
    public string? StudentImageUrl { get; init; }
    public int PropertyId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public string? PropertyImageUrl { get; init; }
    public string AreaNameEn { get; init; } = string.Empty;

    // Six rating categories (1–5 each)
    public byte CleanlinessRating { get; init; }
    public byte SafetyRating { get; init; }
    public byte InternetRating { get; init; }
    public byte LocationRating { get; init; }
    public byte QuietnessRating { get; init; }
    public byte OverallRating { get; init; }

    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Moderation
    public bool IsHidden { get; init; }

    // Computed
    public decimal AverageScore =>
        Math.Round(
            (CleanlinessRating + SafetyRating + InternetRating +
             LocationRating + QuietnessRating + OverallRating) / 6.0m, 2);

    public bool CanEdit =>
        !IsHidden &&
        UpdatedAt == null &&             // only one edit allowed
        (DateTime.UtcNow - CreatedAt).TotalDays <= 30;

    public string TimeAgo => GetTimeAgo(CreatedAt);

    private static string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        return span.TotalMinutes < 1 ? "Just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays}d ago"
            : span.TotalDays < 30 ? $"{(int)span.TotalDays} days ago"
            : dt.ToString("MMM dd, yyyy");
    }
}