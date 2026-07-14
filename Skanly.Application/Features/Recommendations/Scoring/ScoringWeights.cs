// Skanly.Application/Features/Recommendations/Scoring/ScoringWeights.cs
namespace Skanly.Application.Features.Recommendations.Scoring;

/// <summary>
/// All weights sum to 1.0.
/// Adjust these constants to tune recommendation quality.
/// </summary>
public static class ScoringWeights
{
    // ── Signal weights (how much each behavioral source contributes) ───────────
    public const double FavoritesSignal = 0.35;
    public const double SearchHistorySignal = 0.30;
    public const double BookingHistorySignal = 0.25;
    public const double UniversitySignal = 0.10;

    // ── Feature weights within a property score ────────────────────────────────
    public const double Price = 0.30;
    public const double Location = 0.20;
    public const double PropertyType = 0.12;
    public const double Amenities = 0.12;
    public const double UniversityProximity = 0.10;
    public const double Quality = 0.09;   // rating
    public const double GenderPolicy = 0.05;
    public const double Popularity = 0.02;

    // ── Price tolerance band ───────────────────────────────────────────────────
    /// <summary>
    /// Properties within ±20% of the student's inferred price
    /// get a full price score; beyond 50% they get 0.
    /// </summary>
    public const double PriceTolerancePct = 0.20;
    public const double PriceMaxDeviationPct = 0.50;

    // ── Proximity thresholds ───────────────────────────────────────────────────
    public const double WalkingRadiusKm = 1.0;
    public const double NearRadiusKm = 3.0;
    public const double FarRadiusKm = 7.0;
}