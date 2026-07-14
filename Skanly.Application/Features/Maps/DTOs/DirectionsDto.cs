// Skanly.Application/Features/Maps/DTOs/DirectionsDto.cs
namespace Skanly.Application.Features.Maps.DTOs;

public class DirectionsDto
{
    public bool HasRoute { get; init; }
    public string? EncodedPolyline { get; init; }
    public string? TotalDistance { get; init; }
    public string? TotalDuration { get; init; }
    public string TravelMode { get; init; } = "DRIVING";
    public IReadOnlyList<DirectionStepDto> Steps { get; init; }
        = new List<DirectionStepDto>();
}

public class DirectionStepDto
{
    public string Instruction { get; init; } = string.Empty;
    public string Distance { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string TravelMode { get; init; } = string.Empty;
}