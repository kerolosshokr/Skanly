// Skanly.Application/Features/Recommendations/DTOs/RecommendationRequestDto.cs
namespace Skanly.Application.Features.Recommendations.DTOs;

/// <summary>
/// Optional overrides that the student can apply to their recommendations
/// (e.g. from a "Refine Recommendations" UI).
/// </summary>
public class RecommendationRequestDto
{
    public decimal? MaxPrice { get; set; }
    public int? AreaId { get; set; }
    public int? MaxResults { get; set; }
    public bool IncludeExplanations { get; set; } = true;
    public bool ForceRefresh { get; set; } = false;
}