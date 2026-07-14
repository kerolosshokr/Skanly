// Skanly.Application/Features/Recommendations/DTOs/StudentPreferenceProfileDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Recommendations.DTOs;

/// <summary>
/// Aggregated behavioral profile built from a student's
/// search history, favorites, and booking history.
/// Used as the input to the scoring algorithm.
/// </summary>
public class StudentPreferenceProfileDto
{
    public string StudentId { get; init; } = string.Empty;
    public string? StudentFullName { get; init; }
    public int? UniversityId { get; init; }
    public string? UniversityNameEn { get; init; }
    public decimal? UniversityLatitude { get; init; }
    public decimal? UniversityLongitude { get; init; }

    // ── Price Preferences ─────────────────────────────────────────────────────

    /// <summary>Median price across favorited properties.</summary>
    public decimal? FavoriteMedianPrice { get; init; }

    /// <summary>Median price across searched properties (from search filters).</summary>
    public decimal? SearchMedianMinPrice { get; init; }
    public decimal? SearchMedianMaxPrice { get; init; }

    /// <summary>Price of confirmed/accepted bookings (strongest signal).</summary>
    public decimal? BookingMedianPrice { get; init; }

    /// <summary>Combined estimated comfortable price range.</summary>
    public decimal? InferredMinPrice { get; init; }
    public decimal? InferredMaxPrice { get; init; }

    // ── Location Preferences ──────────────────────────────────────────────────

    /// <summary>Area IDs that appear most in favorites + searches.</summary>
    public IReadOnlyList<int> PreferredAreaIds { get; init; }
        = new List<int>();

    public IReadOnlyList<string> PreferredAreaNames { get; init; }
        = new List<string>();

    // ── Property Type Preferences ─────────────────────────────────────────────

    public IReadOnlyList<PropertyType> PreferredPropertyTypes { get; init; }
        = new List<PropertyType>();

    // ── Amenity Preferences ───────────────────────────────────────────────────

    /// <summary>Amenity IDs that appear in favorited/booked properties.</summary>
    public IReadOnlyList<int> PreferredAmenityIds { get; init; }
        = new List<int>();

    // ── Gender Policy Preferences ─────────────────────────────────────────────


    // ── Behavioral Signal Strengths ───────────────────────────────────────────

    public int TotalFavorites { get; init; }
    public int TotalSearches { get; init; }
    public int TotalBookings { get; init; }
    public int TotalViewedProperties { get; init; }

    /// <summary>
    /// How confident we are in this profile.
    /// Low = student is new, use generic recommendations.
    /// </summary>
    public double ProfileConfidence =>
        Math.Min(100.0,
            (TotalFavorites * 3.0) +
            (TotalBookings * 5.0) +
            (TotalSearches * 1.0));

    public bool HasEnoughData => ProfileConfidence >= 10.0;

    public DateTime BuiltAt { get; init; } = DateTime.UtcNow;
}