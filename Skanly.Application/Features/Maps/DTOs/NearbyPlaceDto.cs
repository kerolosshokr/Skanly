// Skanly.Application/Features/Maps/DTOs/NearbyPlaceDto.cs
namespace Skanly.Application.Features.Maps.DTOs;

public class NearbyPlaceDto
{
    public string PlaceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Vicinity { get; init; }
    public string PlaceType { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public double DistanceMeters { get; init; }
    public string DistanceText => DistanceMeters < 1000
        ? $"{(int)DistanceMeters}m"
        : $"{DistanceMeters / 1000:F1}km";
    public double? Rating { get; init; }
    public bool IsOpen { get; init; }

    public string MapIcon => PlaceType switch
    {
        "subway_station" => "🚇",
        "bus_station" => "🚌",
        "train_station" => "🚉",
        "transit_station" => "🚊",
        "supermarket" => "🛒",
        "hospital" => "🏥",
        "pharmacy" => "💊",
        "restaurant" => "🍽️",
        "cafe" => "☕",
        "bank" => "🏦",
        "atm" => "💳",
        "gym" => "💪",
        "mosque" => "🕌",
        "church" => "⛪",
        _ => "📍"
    };
}