// Skanly.Application/Features/Universities/DTOs/UniversityDto.cs
namespace Skanly.Application.Features.Universities.DTOs;

/// <summary>
/// Read-only DTO returned to the presentation layer.
/// Never expose the domain entity directly to views.
/// </summary>
public record UniversityDto
{
    public int UniversityId { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? Address { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    // Aggregated stats (populated by service, not EF Include)
    public int TotalProperties { get; init; }
    public int TotalStudents { get; init; }
}