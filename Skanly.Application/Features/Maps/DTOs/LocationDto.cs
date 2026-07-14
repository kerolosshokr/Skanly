// Skanly.Application/Features/Maps/DTOs/LocationDto.cs
namespace Skanly.Application.Features.Maps.DTOs;

public class LocationDto
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string? Label { get; init; }
    public string? Address { get; init; }

    public static LocationDto From(decimal lat, decimal lng,
        string? label = null, string? address = null)
        => new()
        {
            Latitude = lat,
            Longitude = lng,
            Label = label,
            Address = address
        };
}