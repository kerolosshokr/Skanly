// Skanly.Application/Features/Recommendations/DTOs/PropertyScoreDto.cs
namespace Skanly.Application.Features.Recommendations.DTOs;

/// <summary>
/// Intermediate scoring result for a single property.
/// Used internally by the scoring algorithm before building RecommendationDto.
/// </summary>
public class PropertyScoreDto
{
    public int PropertyId { get; init; }

    // ── Component Scores (0–100 each) ─────────────────────────────────────────

    public double PriceScore { get; init; }
    public double LocationScore { get; init; }
    public double PropertyTypeScore { get; init; }
    public double AmenityScore { get; init; }
    public double UniversityProximityScore { get; init; }
    public double PopularityScore { get; init; }
    public double QualityScore { get; init; }     // rating-based

    // ── Weighted Total ─────────────────────────────────────────────────────────

    public double TotalScore { get; init; }

    // ── Match Reasons (human-readable) ────────────────────────────────────────

    public IReadOnlyList<string> MatchReasons { get; init; }
        = new List<string>();
}