// Skanly.Application/Features/Maps/DTOs/DistanceResultDto.cs
namespace Skanly.Application.Features.Maps.DTOs;

public class DistanceResultDto
{
    public string OriginAddress { get; init; } = string.Empty;
    public string DestinationAddress { get; init; } = string.Empty;

    // Driving
    public double? DrivingDistanceMeters { get; init; }
    public string? DrivingDistanceText { get; init; }
    public string? DrivingDurationText { get; init; }

    // Walking
    public double? WalkingDistanceMeters { get; init; }
    public string? WalkingDistanceText { get; init; }
    public string? WalkingDurationText { get; init; }

    // Transit
    public double? TransitDistanceMeters { get; init; }
    public string? TransitDistanceText { get; init; }
    public string? TransitDurationText { get; init; }

    // Computed
    public double? StraightLineDistanceKm { get; init; }

    public string ClosestModeText =>
        WalkingDistanceMeters < 1000
            ? $"🚶 {WalkingDistanceText} walk"
            : DrivingDistanceText is not null
                ? $"🚗 {DrivingDistanceText} drive"
                : $"📍 {StraightLineDistanceKm:F1} km away";
}