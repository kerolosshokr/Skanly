// Skanly.Application/Features/Properties/DTOs/PropertyCardDto.cs
namespace Skanly.Application.Features.Properties.DTOs;

public class PropertyCardDto
{
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public string? UniversityNameEn { get; init; }
    public decimal PricePerMonth { get; init; }
    public string PropertyTypeDisplay { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public bool IsFavorited { get; init; }
}