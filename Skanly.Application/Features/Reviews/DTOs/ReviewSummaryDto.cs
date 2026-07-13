// Skanly.Application/Features/Reviews/DTOs/ReviewSummaryDto.cs
namespace Skanly.Application.Features.Reviews.DTOs;

/// <summary>
/// Aggregated rating summary for a property — displayed on the
/// property detail page alongside the individual review cards.
/// </summary>
public class ReviewSummaryDto
{
    public int PropertyId { get; init; }
    public int TotalReviews { get; init; }
    public decimal OverallAverage { get; init; }

    // Per-category averages
    public decimal CleanlinessAverage { get; init; }
    public decimal SafetyAverage { get; init; }
    public decimal InternetAverage { get; init; }
    public decimal LocationAverage { get; init; }
    public decimal QuietnessAverage { get; init; }

    // Star distribution (how many reviews gave each star)
    public IReadOnlyDictionary<int, int> StarDistribution { get; init; }
        = new Dictionary<int, int>
        {
            [5] = 0,
            [4] = 0,
            [3] = 0,
            [2] = 0,
            [1] = 0
        };

    public decimal PercentageForStar(int star)
    {
        if (TotalReviews == 0) return 0;
        return StarDistribution.TryGetValue(star, out var count)
            ? Math.Round((decimal)count / TotalReviews * 100, 1)
            : 0;
    }
}