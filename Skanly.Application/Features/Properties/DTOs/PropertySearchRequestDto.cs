using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Properties.DTOs;

public class PropertySearchRequestDto
{
    public int? UniversityId { get; set; }
    public int? AreaId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public PropertyType? PropertyType { get; set; }
    public GenderPolicy? GenderPolicy { get; set; }
    public bool? SmokingAllowed { get; set; }
    public int? MinRooms { get; set; }
    public int? MinBeds { get; set; }
    public decimal? MinRating { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "Newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
