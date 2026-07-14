// Skanly.Application/Features/Maps/DTOs/PropertyMapDto.cs
namespace Skanly.Application.Features.Maps.DTOs;

/// <summary>
/// Lightweight pin data sent to the browser for rendering
/// multiple property markers on the explore map.
/// </summary>
public class PropertyMapDto
{
    public int PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal PricePerMonth { get; set; }
    public string PropertyTypeDisplay { get; set; } = string.Empty;
    public string AreaNameEn { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public string? PrimaryImageUrl { get; init; }
    public bool IsAvailable { get; init; }
}