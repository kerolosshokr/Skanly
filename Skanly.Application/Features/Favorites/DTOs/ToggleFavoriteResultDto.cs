// Skanly.Application/Features/Favorites/DTOs/ToggleFavoriteResultDto.cs
namespace Skanly.Application.Features.Favorites.DTOs;

/// <summary>
/// Returned by the toggle endpoint so the client knows the
/// new state without a page reload.
/// </summary>
public class ToggleFavoriteResultDto
{
    public bool IsFavorited { get; init; }
    public int TotalFavorites { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ToggleFavoriteResultDto Added(int total) => new()
    {
        IsFavorited = true,
        TotalFavorites = total,
        Message = "Added to saved homes."
    };

    public static ToggleFavoriteResultDto Removed(int total) => new()
    {
        IsFavorited = false,
        TotalFavorites = total,
        Message = "Removed from saved homes."
    };
}