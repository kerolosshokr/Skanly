using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IPropertyRepository : IRepository<Property>
{
    Task<(IReadOnlyList<Property> Items, int TotalCount)> SearchAsync(
        PropertySearchFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    Task<Property?> GetDetailAsync(int propertyId, CancellationToken ct = default);

    Task<IReadOnlyList<Property>> GetByOwnerIdAsync(
        string ownerId,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<Property>> GetPendingApprovalAsync(CancellationToken ct = default);

    Task RecalculateAverageRatingAsync(int propertyId, CancellationToken ct = default);

    Task<IReadOnlyList<Property>> GetNearbyAsync(
        decimal latitude,
        decimal longitude,
        double radiusKm,
        CancellationToken ct = default);
}

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
    public string SortBy { get; set; } = "Newest";
}