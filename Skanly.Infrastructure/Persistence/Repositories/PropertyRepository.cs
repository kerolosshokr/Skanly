using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class PropertyRepository : GenericRepository<Property>, IPropertyRepository
{
    public PropertyRepository(SkanlyDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Property> Items, int TotalCount)> SearchAsync(
        PropertySearchFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(p => p.IsApproved && p.IsAvailable)
            .Include(p => p.Area)
            .Include(p => p.University)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.PropertyAmenities)
                .ThenInclude(pa => pa.Amenity)
            .AsQueryable();

        if (filter.UniversityId.HasValue)
            query = query.Where(p => p.UniversityId == filter.UniversityId);

        if (filter.AreaId.HasValue)
            query = query.Where(p => p.AreaId == filter.AreaId);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.PricePerMonth >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.PricePerMonth <= filter.MaxPrice);

        if (filter.PropertyType.HasValue)
            query = query.Where(p => p.PropertyType == filter.PropertyType);

        if (filter.GenderPolicy.HasValue)
            query = query.Where(p => p.GenderPolicy == filter.GenderPolicy);

        if (filter.SmokingAllowed.HasValue)
            query = query.Where(p => p.SmokingAllowed == filter.SmokingAllowed);

        if (filter.MinRooms.HasValue)
            query = query.Where(p => p.Rooms >= filter.MinRooms);

        if (filter.MinBeds.HasValue)
            query = query.Where(p => p.Beds >= filter.MinBeds);

        if (filter.MinRating.HasValue)
            query = query.Where(p => p.AverageRating >= filter.MinRating);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(term) ||
                p.Address.ToLower().Contains(term));
        }

        if (filter.AmenityIds.Any())
        {
            foreach (var amenityId in filter.AmenityIds)
            {
                query = query.Where(p =>
                    p.PropertyAmenities.Any(pa => pa.AmenityId == amenityId));
            }
        }

        query = filter.SortBy switch
        {
            "PriceLow" => query.OrderBy(p => p.PricePerMonth),
            "PriceHigh" => query.OrderByDescending(p => p.PricePerMonth),
            "Rating" => query.OrderByDescending(p => p.AverageRating),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Property?> GetDetailAsync(int propertyId, CancellationToken ct = default)
        => await _dbSet
            .Include(p => p.Owner)
            .Include(p => p.University)
            .Include(p => p.Area)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .Include(p => p.PropertyAmenities).ThenInclude(pa => pa.Amenity)
            .Include(p => p.Reviews).ThenInclude(r => r.Student)
            .FirstOrDefaultAsync(p => p.Id == propertyId, ct);

    public async Task<IReadOnlyList<Property>> GetByOwnerIdAsync(
        string ownerId,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().Where(p => p.OwnerId == ownerId)
            : _dbSet.Where(p => p.OwnerId == ownerId);

        return await query
            .Include(p => p.Area)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Property>> GetPendingApprovalAsync(CancellationToken ct = default)
        => await _dbSet
            .IgnoreQueryFilters()
            .Where(p => !p.IsApproved && !p.IsDeleted)
            .Include(p => p.Owner)
            .Include(p => p.Area)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task RecalculateAverageRatingAsync(int propertyId, CancellationToken ct = default)
    {
        var avg = await _context.Reviews
            .Where(r => r.PropertyId == propertyId)
            .AverageAsync(r => (double?)
                ((r.CleanlinessRating + r.SafetyRating + r.InternetRating +
                  r.LocationRating + r.QuietnessRating + r.OverallRating) / 6.0), ct);

        var property = await _dbSet.FindAsync(new object[] { propertyId }, ct);
        if (property is not null)
        {
            property.AverageRating = (decimal)(avg ?? 0);
            _dbSet.Update(property);
        }
    }

    public async Task<IReadOnlyList<Property>> GetNearbyAsync(
        decimal latitude,
        decimal longitude,
        double radiusKm,
        CancellationToken ct = default)
    {
        const double EarthRadiusKm = 6371.0;
        double lat = (double)latitude;
        double lng = (double)longitude;

        return await _dbSet
            .AsNoTracking()
            .Where(p => p.IsApproved && p.IsAvailable)
            .Where(p =>
                EarthRadiusKm * 2 * Math.Asin(Math.Sqrt(
                    Math.Pow(Math.Sin(((double)p.Latitude - lat) * Math.PI / 360), 2) +
                    Math.Cos(lat * Math.PI / 180) *
                    Math.Cos((double)p.Latitude * Math.PI / 180) *
                    Math.Pow(Math.Sin(((double)p.Longitude - lng) * Math.PI / 360), 2)
                )) <= radiusKm)
            .Include(p => p.Area)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .ToListAsync(ct);
    }
}