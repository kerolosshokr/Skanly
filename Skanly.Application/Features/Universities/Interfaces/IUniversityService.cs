// Skanly.Application/Features/Universities/Interfaces/IUniversityService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Universities.DTOs;

namespace Skanly.Application.Features.Universities.Interfaces;

public interface IUniversityService
{
    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all universities paged — for Admin management grid.</summary>
    Task<ServiceResult<PagedResult<UniversityDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        bool? isActive = null,
        CancellationToken ct = default);

    /// <summary>Returns all active universities as a flat list — for dropdowns.</summary>
    Task<ServiceResult<IReadOnlyList<UniversityDto>>> GetActiveListAsync(
        CancellationToken ct = default);

    /// <summary>Returns a single university by ID with aggregated stats.</summary>
    Task<ServiceResult<UniversityDto>> GetByIdAsync(
        int universityId,
        CancellationToken ct = default);

    /// <summary>Returns the most popular universities for the analytics dashboard.</summary>
    Task<ServiceResult<IReadOnlyList<UniversityDto>>> GetMostPopularAsync(
        int top = 5,
        CancellationToken ct = default);

    // ── Commands ──────────────────────────────────────────────────────────────

    Task<ServiceResult<UniversityDto>> CreateAsync(
        CreateUniversityDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<UniversityDto>> UpdateAsync(
        UpdateUniversityDto dto,
        CancellationToken ct = default);

    Task<ServiceResult> ToggleActiveAsync(
        int universityId,
        CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(
        int universityId,
        CancellationToken ct = default);
}