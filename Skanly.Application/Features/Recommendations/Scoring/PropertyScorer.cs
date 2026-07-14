// Skanly.Application/Features/Recommendations/Scoring/PropertyScorer.cs
using Skanly.Application.Features.Maps.Interfaces;
using Skanly.Application.Features.Recommendations.DTOs;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Recommendations.Scoring;

/// <summary>
/// Pure scoring logic — no DB calls, no external services.
/// Takes a property and a preference profile and returns a score 0–100.
///
/// Every score component is independently unit-testable.
/// </summary>
public static class PropertyScorer
{
    public static PropertyScoreDto Score(
        Property property,
        StudentPreferenceProfileDto profile,
        IGoogleMapsService mapsService)
    {
        var reasons = new List<string>();

        // ── 1. Price Score ────────────────────────────────────────────────────
        var priceScore = ScorePrice(
            property.PricePerMonth,
            profile.InferredMinPrice,
            profile.InferredMaxPrice,
            reasons);

        // ── 2. Location Score ─────────────────────────────────────────────────
        var locationScore = ScoreLocation(
            property.Area.Id,
            profile.PreferredAreaIds,
            reasons);

        // ── 3. Property Type Score ────────────────────────────────────────────
        var typeScore = ScorePropertyType(
            property.PropertyType,
            profile.PreferredPropertyTypes,
            reasons);

        // ── 4. Amenity Score ──────────────────────────────────────────────────
        var amenityScore = ScoreAmenities(
           property.PropertyAmenities.Select(a => a.AmenityId)
           .ToList(),
            profile.PreferredAmenityIds,
            reasons);

        // ── 5. University Proximity Score ─────────────────────────────────────
        var proximityScore = 0.0;
        string? distanceText = null;
        if (profile.UniversityLatitude.HasValue &&
            profile.UniversityLongitude.HasValue)
        {
            var distKm = mapsService.GetStraightLineDistanceKm(
                property.Latitude, property.Longitude,
                profile.UniversityLatitude.Value,
                profile.UniversityLongitude.Value);

            proximityScore = ScoreUniversityProximity(distKm, reasons);
            distanceText = distKm < 1.0
                ? $"{(int)(distKm * 1000)}m from university"
                : $"{distKm:F1}km from university";
        }

        // ── 6. Quality Score (rating-based) ───────────────────────────────────
        var qualityScore = ScoreQuality(
            (double)property.AverageRating,
            reasons);

        

        // ── 8. Popularity Score ───────────────────────────────────────────────
        var popularityScore = ScorePopularity(property, reasons);

        // ── 9. Weighted total ─────────────────────────────────────────────────
        var total =
            priceScore * ScoringWeights.Price +
            locationScore * ScoringWeights.Location +
            typeScore * ScoringWeights.PropertyType +
            amenityScore * ScoringWeights.Amenities +
            proximityScore * ScoringWeights.UniversityProximity +
            qualityScore * ScoringWeights.Quality +
   
            popularityScore * ScoringWeights.Popularity;

        // Clamp 0–100
        total = Math.Clamp(total, 0.0, 100.0);

        return new PropertyScoreDto
        {
            PropertyId = property.Id,
            PriceScore = priceScore,
            LocationScore = locationScore,
            PropertyTypeScore = typeScore,
            AmenityScore = amenityScore,
            UniversityProximityScore = proximityScore,
            QualityScore = qualityScore,
            PopularityScore = popularityScore,
            TotalScore = total,
            MatchReasons = reasons
        };
    }

    // ── Price Scoring ─────────────────────────────────────────────────────────

    private static double ScorePrice(
        decimal propertyPrice,
        decimal? minPrice,
        decimal? maxPrice,
        List<string> reasons)
    {
        // No price signal → neutral score
        if (!minPrice.HasValue && !maxPrice.HasValue)
            return 60.0;

        var min = minPrice ?? 0m;
        var max = maxPrice ?? propertyPrice * 2m;
        var mid = (min + max) / 2m;

        if (mid == 0) return 60.0;

        var deviation = Math.Abs((double)(propertyPrice - mid) / (double)mid);

        if (deviation <= ScoringWeights.PriceTolerancePct)
        {
            reasons.Add("💰 Within your typical price range");
            return 100.0;
        }

        if (deviation <= ScoringWeights.PriceMaxDeviationPct)
        {
            var score = 100.0 * (1.0 -
                (deviation - ScoringWeights.PriceTolerancePct) /
                (ScoringWeights.PriceMaxDeviationPct -
                 ScoringWeights.PriceTolerancePct));

            if (propertyPrice < min)
                reasons.Add("💰 Below your usual budget — good value");
            return Math.Max(score, 0.0);
        }

        return 0.0;  // Too far from preferred price
    }

    // ── Location Scoring ──────────────────────────────────────────────────────

    private static double ScoreLocation(
        int propertyAreaId,
        IReadOnlyList<int> preferredAreaIds,
        List<string> reasons)
    {
        if (!preferredAreaIds.Any()) return 55.0;

        var rank = preferredAreaIds.ToList().IndexOf(propertyAreaId);

        if (rank == 0)
        {
            reasons.Add("📍 In your most preferred area");
            return 100.0;
        }
        if (rank == 1)
        {
            reasons.Add("📍 In one of your preferred areas");
            return 85.0;
        }
        if (rank >= 2 && rank <= 4)
        {
            reasons.Add("📍 Area you've shown interest in");
            return 65.0;
        }

        return 30.0;  // Not a preferred area but not penalized heavily
    }

    // ── Property Type Scoring ─────────────────────────────────────────────────

    private static double ScorePropertyType(
        PropertyType propertyType,
        IReadOnlyList<PropertyType> preferredTypes,
        List<string> reasons)
    {
        if (!preferredTypes.Any()) return 60.0;

        var rank = preferredTypes.ToList().IndexOf(propertyType);

        if (rank == 0)
        {
            reasons.Add($"🏠 Matches your preferred type ({propertyType})");
            return 100.0;
        }
        if (rank == 1) return 70.0;
        if (rank >= 2) return 40.0;

        return 20.0;
    }

    // ── Amenity Scoring ───────────────────────────────────────────────────────

    private static double ScoreAmenities(
        IReadOnlyList<int> propertyAmenityIds,
        IReadOnlyList<int> preferredAmenityIds,
        List<string> reasons)
    {
        if (!preferredAmenityIds.Any()) return 50.0;
        if (!propertyAmenityIds.Any()) return 10.0;

        var matching = propertyAmenityIds
            .Intersect(preferredAmenityIds)
            .Count();

        var coverage = (double)matching / preferredAmenityIds.Count;

        if (coverage >= 0.80)
        {
            reasons.Add($"✨ Has {matching} of your preferred amenities");
            return 100.0;
        }
        if (coverage >= 0.50)
        {
            reasons.Add($"✨ Has {matching} of your preferred amenities");
            return 65.0 + (coverage * 35.0);
        }
        if (coverage >= 0.25)
            return 35.0 + (coverage * 30.0);

        return coverage * 35.0;
    }

    // ── University Proximity Scoring ──────────────────────────────────────────

    private static double ScoreUniversityProximity(
        double distKm,
        List<string> reasons)
    {
        if (distKm <= ScoringWeights.WalkingRadiusKm)
        {
            reasons.Add("🎓 Walking distance from university");
            return 100.0;
        }
        if (distKm <= ScoringWeights.NearRadiusKm)
        {
            reasons.Add("🎓 Close to university");
            return 75.0;
        }
        if (distKm <= ScoringWeights.FarRadiusKm)
        {
            return 40.0;
        }

        return Math.Max(0.0, 40.0 - (distKm - ScoringWeights.FarRadiusKm) * 3.0);
    }

    // ── Quality Scoring ───────────────────────────────────────────────────────

    private static double ScoreQuality(
        double rating,
        List<string> reasons)
    {
        if (rating == 0) return 50.0;  // No reviews yet — neutral

        var score = (rating / 5.0) * 100.0;

        if (rating >= 4.5)
            reasons.Add($"⭐ Highly rated ({rating:F1}/5)");
        else if (rating >= 4.0)
            reasons.Add($"⭐ Well reviewed ({rating:F1}/5)");

        return score;
    }

    // ── Gender Policy Scoring ─────────────────────────────────────────────────


    // ── Popularity Scoring ────────────────────────────────────────────────────

    private static double ScorePopularity(
        Property property,
        List<string> reasons)
    {
        var totalReviews = property.Reviews?.Count ?? 0;

        var reviewScore = Math.Min(totalReviews / 10.0, 1.0) * 100.0;

        if (totalReviews >= 10)
            reasons.Add($"🔥 Popular property ({totalReviews} reviews)");

        return reviewScore;
    }
}