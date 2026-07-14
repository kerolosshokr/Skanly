// Skanly.Application/Features/Recommendations/Interfaces/IRecommendationService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Recommendations.DTOs;

namespace Skanly.Application.Features.Recommendations.Interfaces;

public interface IRecommendationService
{
    /// <summary>
    /// Returns personalised property recommendations for a student.
    /// Pipeline: build profile → score candidates → AI refinement → rank.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<RecommendationDto>>> GetRecommendationsAsync(
        string studentId,
        RecommendationRequestDto? request = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the student's current preference profile
    /// (for debugging or displaying to the user).
    /// </summary>
    Task<ServiceResult<StudentPreferenceProfileDto>> GetPreferenceProfileAsync(
        string studentId,
        CancellationToken ct = default);

    /// <summary>
    /// Records that a student searched with specific filters.
    /// Called by PropertyController.Search after every search.
    /// </summary>
    Task RecordSearchAsync(
        string studentId,
        StudentSearchHistoryEntry entry,
        CancellationToken ct = default);
}

public class StudentSearchHistoryEntry
{
    public int? UniversityId { get; init; }
    public int? AreaId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public Skanly.Domain.Enums.PropertyType? PropertyType { get; init; }
    public DateTime SearchedAt { get; init; } = DateTime.UtcNow;
}