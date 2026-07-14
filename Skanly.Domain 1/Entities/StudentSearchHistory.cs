// Skanly.Domain/Entities/StudentSearchHistory.cs
using Skanly.Domain.Enums;

namespace Skanly.Domain.Entities;

/// <summary>
/// Records each search a student performs.
/// Used by the recommendation engine to infer preferences.
/// Automatically cleaned up after 90 days via a background job.
/// </summary>
public class StudentSearchHistory
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;


    // Search filter snapshot
    public int? UniversityId { get; set; }
    public int? AreaId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public PropertyType? PropertyType { get; set; }
 

    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}