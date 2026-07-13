// Skanly.Application/Features/Favorites/DTOs/FavoriteDto.cs
namespace Skanly.Application.Features.Favorites.DTOs;

/// <summary>
/// Full favorite entry returned to the presentation layer.
/// Contains enough property data to render a card without a
/// second query.
/// </summary>
public class FavoriteDto
{
    public int FavoriteId { get; init; }
    public string StudentId { get; init; } = string.Empty;
    public DateTime SavedAt { get; init; }

    // Property snapshot
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public string AreaNameAr { get; init; } = string.Empty;
    public string? UniversityNameEn { get; init; }
    public decimal PricePerMonth { get; init; }
    public string PropertyTypeDisplay { get; init; } = string.Empty;
    public int Rooms { get; init; }
    public int Beds { get; init; }
    public decimal AverageRating { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsApproved { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public string? OwnerFullName { get; init; }
    public bool OwnerIsVerified { get; init; }

    // Computed helpers used by the view
    public string AvailabilityBadgeClass =>
        IsAvailable ? "bg-success" : "bg-secondary";

    public string AvailabilityDisplay =>
        IsAvailable ? "Available" : "Occupied";

    public string TimeAgoSaved => GetTimeAgo(SavedAt);

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