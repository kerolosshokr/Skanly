// Skanly.Application/Features/Favorites/Services/FavoriteService.cs
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Favorites.DTOs;
using Skanly.Application.Features.Favorites.Interfaces;
using Skanly.Domain.Entities;

namespace Skanly.Application.Features.Favorites.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(IUnitOfWork uow, ILogger<FavoriteService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ── ToggleAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<ToggleFavoriteResultDto>> ToggleAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default)
    {
        var alreadyFavorited = await _uow.Favorites
            .IsFavoritedAsync(studentId, propertyId, ct);

        return alreadyFavorited
            ? await RemoveAsync(studentId, propertyId, ct)
            : await AddAsync(studentId, propertyId, ct);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<ToggleFavoriteResultDto>> AddAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default)
    {
        // Guard: property must exist and be approved
        var property = await _uow.Properties
            .GetByIdAsync(propertyId, ct);

        if (property is null || property.IsDeleted)
            return ServiceResult<ToggleFavoriteResultDto>.Failure(
                "Property not found.");

        if (!property.IsApproved)
            return ServiceResult<ToggleFavoriteResultDto>.Failure(
                "This property is not available for saving.");

        // Guard: already favorited (idempotency)
        var alreadyExists = await _uow.Favorites
            .IsFavoritedAsync(studentId, propertyId, ct);

        if (alreadyExists)
        {
            var count = await GetCountAsync(studentId, ct);
            return ServiceResult<ToggleFavoriteResultDto>.Success(
                ToggleFavoriteResultDto.Added(count.Data));
        }

        var favorite = new Favorite
        {
            StudentId = studentId,
            PropertyId = propertyId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Favorites.AddAsync(favorite, ct);
        await _uow.SaveChangesAsync(ct);

        var newCount = await GetCountAsync(studentId, ct);

        _logger.LogInformation(
            "Student {StudentId} saved property {PropertyId}",
            studentId, propertyId);

        return ServiceResult<ToggleFavoriteResultDto>.Success(
            ToggleFavoriteResultDto.Added(newCount.Data));
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<ToggleFavoriteResultDto>> RemoveAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default)
    {
        var existing = await _uow.Favorites
            .GetByStudentAndPropertyAsync(studentId, propertyId, ct);

        if (existing is null)
        {
            var count = await GetCountAsync(studentId, ct);
            return ServiceResult<ToggleFavoriteResultDto>.Success(
                ToggleFavoriteResultDto.Removed(count.Data));
        }

        _uow.Favorites.Remove(existing);
        await _uow.SaveChangesAsync(ct);

        var newCount = await GetCountAsync(studentId, ct);

        _logger.LogInformation(
            "Student {StudentId} removed property {PropertyId} from favorites",
            studentId, propertyId);

        return ServiceResult<ToggleFavoriteResultDto>.Success(
            ToggleFavoriteResultDto.Removed(newCount.Data));
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<FavoriteDto>>> GetPagedAsync(
        string studentId,
        int pageNumber = 1,
        int pageSize = 12,
        string? sortBy = "Newest",
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        // Load all favorites for this student (repository handles eager loading)
        var allFavorites = await _uow.Favorites
            .GetByStudentIdAsync(studentId, ct);

        // Apply search filter
        var filtered = string.IsNullOrWhiteSpace(searchTerm)
            ? allFavorites
            : allFavorites.Where(f =>
                f.Property.Title.Contains(
                    searchTerm, StringComparison.OrdinalIgnoreCase) ||
                f.Property.Area.NameEn.Contains(
                    searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (f.Property.University?.NameEn.Contains(
                    searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        // Apply sort
        var sorted = sortBy switch
        {
            "PriceLow" => filtered.OrderBy(f => f.Property.PricePerMonth),
            "PriceHigh" => filtered.OrderByDescending(f => f.Property.PricePerMonth),
            "Rating" => filtered.OrderByDescending(f => f.Property.AverageRating),
            "Oldest" => filtered.OrderBy(f => f.CreatedAt),
            _ => filtered.OrderByDescending(f => f.CreatedAt)  // "Newest"
        };

        var total = filtered.Count();

        var paged = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();

        return ServiceResult<PagedResult<FavoriteDto>>.Success(
            PagedResult<FavoriteDto>.Create(paged, total, pageNumber, pageSize));
    }

    // ── IsFavoritedAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<bool>> IsFavoritedAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default)
    {
        var result = await _uow.Favorites
            .IsFavoritedAsync(studentId, propertyId, ct);

        return ServiceResult<bool>.Success(result);
    }

    // ── GetCountAsync ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<int>> GetCountAsync(
        string studentId,
        CancellationToken ct = default)
    {
        var count = await _uow.Favorites
            .CountAsync(f => f.StudentId == studentId, ct);

        return ServiceResult<int>.Success(count);
    }

    // ── GetFavoritedPropertyIdsAsync ──────────────────────────────────────────

    public async Task<ServiceResult<HashSet<int>>> GetFavoritedPropertyIdsAsync(
        string studentId,
        CancellationToken ct = default)
    {
        var favorites = await _uow.Favorites
            .GetByStudentIdAsync(studentId, ct);

        var ids = favorites.Select(f => f.PropertyId).ToHashSet();

        return ServiceResult<HashSet<int>>.Success(ids);
    }

    // ── ClearAllAsync ─────────────────────────────────────────────────────────

    public async Task<ServiceResult> ClearAllAsync(
        string studentId,
        CancellationToken ct = default)
    {
        var allFavorites = await _uow.Favorites
            .GetAllAsync(f => f.StudentId == studentId, ct);

        if (!allFavorites.Any())
            return ServiceResult.Success();

        _uow.Favorites.RemoveRange(allFavorites);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Student {StudentId} cleared all {Count} favorites",
            studentId, allFavorites.Count);

        return ServiceResult.Success();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static FavoriteDto MapToDto(Favorite f) => new()
    {
        FavoriteId = f.Id,
        StudentId = f.StudentId,
        SavedAt = f.CreatedAt,
        PropertyId = f.Property.Id,
        Title = f.Property.Title,
        AreaNameEn = f.Property.Area.NameEn,
        AreaNameAr = f.Property.Area.NameAr,
        UniversityNameEn = f.Property.University?.NameEn,
        PricePerMonth = f.Property.PricePerMonth,
        PropertyTypeDisplay = f.Property.PropertyType.ToString(),
        Rooms = f.Property.Rooms,
        Beds = f.Property.Beds,
        AverageRating = f.Property.AverageRating,
        IsAvailable = f.Property.IsAvailable,
        IsApproved = f.Property.IsApproved,
        PrimaryImageUrl = f.Property.Images
                               .FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                          ?? f.Property.Images.FirstOrDefault()?.ImageUrl,
        OwnerFullName = f.Property.Owner?.FullName,
        OwnerIsVerified = f.Property.Owner?.IsIdentityVerified ?? false
    };
}