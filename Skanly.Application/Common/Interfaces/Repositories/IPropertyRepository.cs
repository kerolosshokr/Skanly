// Skanly.Application/Common/Interfaces/Repositories/IPropertyRepository.cs
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IPropertyRepository : IRepository<Property>
{
    /// <summary>
    /// Smart search with all filters from the spec:
    /// University, Area, Budget, PropertyType, Gender,
    /// Smoking, Amenities, Rooms, Beds.
    /// Returns paged + ordered results for the search page.
    /// </summary>
    Task<(IReadOnlyList<Property> Items, int TotalCount)> SearchAsync(
        PropertySearchFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Returns a property with all navigation data for the detail page.</summary>
    Task<Property?> GetDetailAsync(int propertyId, CancellationToken ct = default);

    /// <summary>Returns all properties for a given owner (including soft-deleted for owner view).</summary>
    Task<IReadOnlyList<Property>> GetByOwnerIdAsync(
        string ownerId,
        bool includeDeleted = false,
        CancellationToken ct = default);

    /// <summary>Returns properties pending admin approval.</summary>
    Task<IReadOnlyList<Property>> GetPendingApprovalAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the denormalised AverageRating column after a review
    /// is created or removed (called from ReviewService).
    /// </summary>
    Task RecalculateAverageRatingAsync(int propertyId, CancellationToken ct = default);

    /// <summary>Returns properties near a location within a radius (km).</summary>
    Task<IReadOnlyList<Property>> GetNearbyAsync(
        decimal latitude,
        decimal longitude,
        double radiusKm,
        CancellationToken ct = default);
}

/// <summary>Filter bag passed to SearchAsync. All fields are optional.</summary>
public class PropertySearchFilter
{
    public int? UniversityId { get; set; }
    public int? AreaId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public PropertyType? PropertyType { get; set; }
    public bool? SmokingAllowed { get; set; }
    public int? MinRooms { get; set; }
    public int? MinBeds { get; set; }
    public decimal? MinRating { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "Newest";  // Newest | PriceLow | PriceHigh | Rating
}