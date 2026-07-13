// Skanly.Application/Features/Favorites/Interfaces/IFavoriteService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Favorites.DTOs;

namespace Skanly.Application.Features.Favorites.Interfaces;

public interface IFavoriteService
{
    // ── Toggle (add if not favorited, remove if favorited) ────────────────────

    /// <summary>
    /// Idempotent toggle. Safe to call from every property card's heart button.
    /// Returns the new favorite state so the UI can update without reload.
    /// </summary>
    Task<ServiceResult<ToggleFavoriteResultDto>> ToggleAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default);

    // ── Explicit add / remove (used internally by Toggle) ────────────────────

    Task<ServiceResult<ToggleFavoriteResultDto>> AddAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default);

    Task<ServiceResult<ToggleFavoriteResultDto>> RemoveAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default);

    // ── Queries ───────────────────────────────────────────────────────────────

    Task<ServiceResult<PagedResult<FavoriteDto>>> GetPagedAsync(
        string studentId,
        int pageNumber = 1,
        int pageSize = 12,
        string? sortBy = "Newest",
        string? searchTerm = null,
        CancellationToken ct = default);

    Task<ServiceResult<bool>> IsFavoritedAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default);

    Task<ServiceResult<int>> GetCountAsync(
        string studentId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a set of favorited property IDs for a student.
    /// Used to bulk-flag cards on a search results page without
    /// N+1 queries.
    /// </summary>
    Task<ServiceResult<HashSet<int>>> GetFavoritedPropertyIdsAsync(
        string studentId,
        CancellationToken ct = default);

    /// <summary>
    /// Removes ALL favorites for a student.
    /// Triggered from the "Clear All" button in the favorites page.
    /// </summary>
    Task<ServiceResult> ClearAllAsync(
        string studentId,
        CancellationToken ct = default);
}