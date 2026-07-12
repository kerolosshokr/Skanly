// Skanly.Application/Features/Properties/Services/PropertyService.cs
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Properties.Services;

public class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<CreatePropertyDto> _createValidator;
    private readonly IValidator<UpdatePropertyDto> _updateValidator;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(
        IUnitOfWork uow,
        IFileStorageService fileStorage,
        IValidator<CreatePropertyDto> createValidator,
        IValidator<UpdatePropertyDto> updateValidator,
        ILogger<PropertyService> logger)
    {
        _uow = uow;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    // ── SearchAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<PropertyCardDto>>> SearchAsync(
        PropertySearchRequestDto request,
        string? viewerUserId = null,
        CancellationToken ct = default)
    {
        var filter = new PropertySearchFilter
        {
            UniversityId = request.UniversityId,
            AreaId = request.AreaId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            PropertyType = request.PropertyType,
            SmokingAllowed = request.SmokingAllowed,
            MinRooms = request.MinRooms,
            MinBeds = request.MinBeds,
            MinRating = request.MinRating,
            AmenityIds = request.AmenityIds,
            SearchTerm = request.SearchTerm,
            SortBy = request.SortBy
        };

        var (items, total) = await _uow.Properties.SearchAsync(
            filter, request.Page, request.PageSize, ct);

        // Get student's favorite set for IsFavorited flag
        HashSet<int> favoriteIds = new();
        if (!string.IsNullOrEmpty(viewerUserId))
        {
            var favorites = await _uow.Favorites
                .GetByStudentIdAsync(viewerUserId, ct);
            favoriteIds = favorites.Select(f => f.PropertyId).ToHashSet();
        }

        var dtos = items.Select(p => MapToCard(p, favoriteIds)).ToList();

        return ServiceResult<PagedResult<PropertyCardDto>>.Success(
            PagedResult<PropertyCardDto>.Create(dtos, total, request.Page, request.PageSize));
    }

    // ── GetDetailAsync ────────────────────────────────────────────────────────

    public async Task<ServiceResult<PropertyDetailDto>> GetDetailAsync(
        int propertyId,
        string? viewerUserId = null,
        CancellationToken ct = default)
    {
        var property = await _uow.Properties.GetDetailAsync(propertyId, ct);

        if (property is null || property.IsDeleted)
            return ServiceResult<PropertyDetailDto>.Failure("Property not found.");

        bool isFavorited = false;
        bool hasActiveBooking = false;

        if (!string.IsNullOrEmpty(viewerUserId))
        {
            isFavorited = await _uow.Favorites.IsFavoritedAsync(
                viewerUserId, propertyId, ct);

            hasActiveBooking = await _uow.Bookings.ExistsAsync(
                b => b.StudentId == viewerUserId &&
                     b.PropertyId == propertyId &&
                     (b.Status == BookingStatus.Confirmed ||
                      b.Status == BookingStatus.Accepted ||
                      b.Status == BookingStatus.PaymentPending), ct);
        }

        var dto = MapToDetail(property, isFavorited, hasActiveBooking);
        return ServiceResult<PropertyDetailDto>.Success(dto);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<PropertyDetailDto>> CreateAsync(
        string ownerId,
        CreatePropertyDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<PropertyDetailDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Verify owner is identity-verified
        var owner = await _uow.Owners.GetByUserIdAsync(ownerId, ct);
        if (owner is null)
            return ServiceResult<PropertyDetailDto>.Failure("Owner not found.");

        if (!owner.IsIdentityVerified)
            return ServiceResult<PropertyDetailDto>.Failure(
                "You must complete identity verification before listing properties.");

        // 3. Verify area exists
        var area = await _uow.Repository<Area>().GetByIdAsync(dto.AreaId, ct);
        if (area is null)
            return ServiceResult<PropertyDetailDto>.Failure("Selected area not found.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // 4. Create property entity
            var property = new Property
            {
                OwnerId = ownerId,
                UniversityId = dto.UniversityId,
                AreaId = dto.AreaId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                PropertyType = dto.PropertyType,
                SmokingAllowed = dto.SmokingAllowed,
                Rooms = dto.Rooms,
                Beds = dto.Beds,
                PricePerMonth = dto.PricePerMonth,
                Address = dto.Address.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsAvailable = true,
                IsApproved = false    // must go through Admin approval
            };

            await _uow.Repository<Property>().AddAsync(property, ct);
            await _uow.SaveChangesAsync(ct);  // get the new PropertyId

            // 5. Upload images
            for (int i = 0; i < dto.Images.Count; i++)
            {
                var url = await _fileStorage.SaveAsync(
                    dto.Images[i], $"properties/{property.Id}/images", ct);

                await _uow.Repository<PropertyImage>().AddAsync(new PropertyImage
                {
                    PropertyId = property.Id,
                    ImageUrl = url,
                    IsPrimary = (i == dto.PrimaryImageIndex)
                }, ct);
            }

            // 6. Upload videos
            foreach (var video in dto.Videos)
            {
                var url = await _fileStorage.SaveAsync(
                    video, $"properties/{property.Id}/videos", ct);

                await _uow.Repository<PropertyVideo>().AddAsync(new PropertyVideo
                {
                    PropertyId = property.Id,
                    VideoUrl = url
                }, ct);
            }

            // 7. Wire amenities
            foreach (var amenityId in dto.AmenityIds.Distinct())
            {
                await _uow.Repository<PropertyAmenity>().AddAsync(new PropertyAmenity
                {
                    PropertyId = property.Id,
                    AmenityId = amenityId
                }, ct);
            }

            // 8. Notify admin (queued notification)
            await NotifyAdminPendingPropertyAsync(property, ct);

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Property created: Id={Id} OwnerId={OwnerId}", property.Id, ownerId);

            return await GetDetailAsync(property.Id, ownerId, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<PropertyDetailDto>> UpdateAsync(
        string ownerId,
        UpdatePropertyDto dto,
        CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<PropertyDetailDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        var property = await _uow.Properties.GetDetailAsync(dto.PropertyId, ct);
        if (property is null || property.IsDeleted)
            return ServiceResult<PropertyDetailDto>.Failure("Property not found.");

        if (property.OwnerId != ownerId)
            return ServiceResult<PropertyDetailDto>.Failure("Access denied.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // Update scalar fields
            property.Title = dto.Title.Trim();
            property.Description = dto.Description?.Trim();
            property.PropertyType = dto.PropertyType;
            property.SmokingAllowed = dto.SmokingAllowed;
            property.Rooms = dto.Rooms;
            property.Beds = dto.Beds;
            property.PricePerMonth = dto.PricePerMonth;
            property.Address = dto.Address.Trim();
            property.Latitude = dto.Latitude;
            property.Longitude = dto.Longitude;
            property.UniversityId = dto.UniversityId;
            property.AreaId = dto.AreaId;
            property.IsAvailable = dto.IsAvailable;

            // After editing, send back for re-approval if previously approved
            if (property.IsApproved)
            {
                property.IsApproved = false;
                await NotifyAdminPendingPropertyAsync(property, ct);
            }

            _uow.Repository<Property>().Update(property);

            // Delete removed images
            foreach (var imgId in dto.DeleteImageIds)
            {
                var img = await _uow.Repository<PropertyImage>().GetByIdAsync(imgId, ct);
                if (img is not null && img.PropertyId == property.Id)
                {
                    await _fileStorage.DeleteAsync(img.ImageUrl, ct);
                    _uow.Repository<PropertyImage>().Remove(img);
                }
            }

            // Upload new images
            foreach (var file in dto.NewImages)
            {
                var url = await _fileStorage.SaveAsync(
                    file, $"properties/{property.Id}/images", ct);
                await _uow.Repository<PropertyImage>().AddAsync(new PropertyImage
                {
                    PropertyId = property.Id,
                    ImageUrl = url,
                    IsPrimary = false
                }, ct);
            }

            // Upload new videos
            foreach (var file in dto.NewVideos)
            {
                var url = await _fileStorage.SaveAsync(
                    file, $"properties/{property.Id}/videos", ct);
                await _uow.Repository<PropertyVideo>().AddAsync(new PropertyVideo
                {
                    PropertyId = property.Id,
                    VideoUrl = url
                }, ct);
            }

            // Set primary image
            if (dto.PrimaryImageId.HasValue)
            {
                var allImages = await _uow.Repository<PropertyImage>()
                    .GetAllAsync(i => i.PropertyId == property.Id, ct);

                foreach (var img in allImages)
                {
                    img.IsPrimary = (img.Id == dto.PrimaryImageId.Value);
                    _uow.Repository<PropertyImage>().Update(img);
                }
            }

            // Sync amenities — remove all then re-add
            var existingAmenities = await _uow.Repository<PropertyAmenity>()
                .GetAllAsync(pa => pa.PropertyId == property.Id, ct);
            _uow.Repository<PropertyAmenity>().RemoveRange(existingAmenities);

            foreach (var amenityId in dto.AmenityIds.Distinct())
            {
                await _uow.Repository<PropertyAmenity>().AddAsync(new PropertyAmenity
                {
                    PropertyId = property.Id,
                    AmenityId = amenityId
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Property updated: Id={Id}", property.Id);

            return await GetDetailAsync(property.Id, ownerId, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── SoftDeleteAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult> SoftDeleteAsync(
        string ownerId,
        int propertyId,
        CancellationToken ct = default)
    {
        var property = await _uow.Repository<Property>()
            .GetByIdAsync(propertyId, ct);

        if (property is null)
            return ServiceResult.Failure("Property not found.");

        if (property.OwnerId != ownerId)
            return ServiceResult.Failure("Access denied.");

        // Block delete if active confirmed bookings exist
        var hasActiveBookings = await _uow.Bookings.ExistsAsync(
            b => b.PropertyId == propertyId &&
                 (b.Status == BookingStatus.Confirmed ||
                  b.Status == BookingStatus.Accepted), ct);

        if (hasActiveBookings)
            return ServiceResult.Failure(
                "Cannot delete a property with active bookings. " +
                "Please resolve all active bookings first.");

        property.IsDeleted = true;
        property.IsAvailable = false;
        _uow.Repository<Property>().Update(property);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Property soft-deleted: Id={Id}", propertyId);
        return ServiceResult.Success();
    }

    // ── SetPrimaryImageAsync ──────────────────────────────────────────────────

    public async Task<ServiceResult> SetPrimaryImageAsync(
        string ownerId,
        int propertyId,
        int imageId,
        CancellationToken ct = default)
    {
        var property = await _uow.Repository<Property>()
            .GetByIdAsync(propertyId, ct);

        if (property is null || property.OwnerId != ownerId)
            return ServiceResult.Failure("Access denied.");

        var allImages = await _uow.Repository<PropertyImage>()
            .GetAllAsync(i => i.PropertyId == propertyId, ct);

        bool found = false;
        foreach (var img in allImages)
        {
            img.IsPrimary = (img.Id == imageId);
            if (img.Id == imageId) found = true;
            _uow.Repository<PropertyImage>().Update(img);
        }

        if (!found) return ServiceResult.Failure("Image not found.");

        await _uow.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    // ── DeleteImageAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult> DeleteImageAsync(
        string ownerId,
        int propertyId,
        int imageId,
        CancellationToken ct = default)
    {
        var property = await _uow.Repository<Property>()
            .GetByIdAsync(propertyId, ct);

        if (property is null || property.OwnerId != ownerId)
            return ServiceResult.Failure("Access denied.");

        var image = await _uow.Repository<PropertyImage>().GetByIdAsync(imageId, ct);
        if (image is null || image.PropertyId != propertyId)
            return ServiceResult.Failure("Image not found.");

        // Must keep at least 1 image
        var imageCount = await _uow.Repository<PropertyImage>()
            .CountAsync(i => i.PropertyId == propertyId, ct);

        if (imageCount <= 1)
            return ServiceResult.Failure(
                "A property must have at least one photo. " +
                "Upload a replacement before deleting this one.");

        await _fileStorage.DeleteAsync(image.ImageUrl, ct);
        _uow.Repository<PropertyImage>().Remove(image);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    // ── ToggleAvailabilityAsync ───────────────────────────────────────────────

    public async Task<ServiceResult> ToggleAvailabilityAsync(
        string ownerId,
        int propertyId,
        CancellationToken ct = default)
    {
        var property = await _uow.Repository<Property>()
            .GetByIdAsync(propertyId, ct);

        if (property is null || property.OwnerId != ownerId)
            return ServiceResult.Failure("Access denied.");

        property.IsAvailable = !property.IsAvailable;
        _uow.Repository<Property>().Update(property);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    // ── GetPendingApprovalAsync ───────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<PropertyCardDto>>> GetPendingApprovalAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var pending = await _uow.Properties.GetPendingApprovalAsync(ct);

        var total = pending.Count;
        var paged = pending
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToCard(p, new HashSet<int>()))
            .ToList();

        return ServiceResult<PagedResult<PropertyCardDto>>.Success(
            PagedResult<PropertyCardDto>.Create(paged, total, pageNumber, pageSize));
    }

    // ── ApproveAsync ──────────────────────────────────────────────────────────

    public async Task<ServiceResult> ApproveAsync(
        int propertyId,
        CancellationToken ct = default)
    {
        var property = await _uow.Properties.GetDetailAsync(propertyId, ct);
        if (property is null)
            return ServiceResult.Failure("Property not found.");

        property.IsApproved = true;
        _uow.Repository<Property>().Update(property);

        // Notify owner
        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = property.OwnerId,
            Title = "Property Approved!",
            Message = $"Your property '{property.Title}' has been approved and is now live.",
            Type = NotificationType.PropertyApproval,
            RelatedEntityId = property.Id,
            RelatedEntityType = "Property"
        }, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Property approved: Id={Id}", propertyId);
        return ServiceResult.Success();
    }

    // ── RejectAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult> RejectAsync(
        int propertyId,
        string reason,
        CancellationToken ct = default)
    {
        var property = await _uow.Properties.GetDetailAsync(propertyId, ct);
        if (property is null)
            return ServiceResult.Failure("Property not found.");

        property.IsApproved = false;
        _uow.Repository<Property>().Update(property);

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = property.OwnerId,
            Title = "Property Listing Rejected",
            Message = $"Your listing '{property.Title}' was not approved. Reason: {reason}",
            Type = NotificationType.PropertyApproval,
            RelatedEntityId = property.Id,
            RelatedEntityType = "Property"
        }, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Property rejected: Id={Id} Reason={Reason}", propertyId, reason);
        return ServiceResult.Success();
    }

    // ── GetAllAmenitiesAsync ──────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<AmenityDto>>> GetAllAmenitiesAsync(
        CancellationToken ct = default)
    {
        var amenities = await _uow.Repository<Amenity>().GetAllAsync(ct);
        var dtos = amenities
            .OrderBy(a => a.NameEn)
            .Select(a => new AmenityDto
            {
                AmenityId = a.Id,
                NameEn = a.NameEn,
                NameAr = a.NameAr,
                IconClass = a.IconClass
            })
            .ToList();

        return ServiceResult<IReadOnlyList<AmenityDto>>.Success(dtos);
    }

    // ── GetRelatedAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<PropertyCardDto>>> GetRelatedAsync(
        int propertyId,
        int count = 4,
        CancellationToken ct = default)
    {
        var property = await _uow.Repository<Property>()
            .GetByIdAsync(propertyId, ct);

        if (property is null)
            return ServiceResult<IReadOnlyList<PropertyCardDto>>.Success(
                new List<PropertyCardDto>());

        // Same area, same type, different property
        var filter = new PropertySearchFilter
        {
            AreaId = property.AreaId,
            PropertyType = property.PropertyType,
            SortBy = "Rating"
        };

        var (related, _) = await _uow.Properties.SearchAsync(filter, 1, count + 1, ct);

        var dtos = related
            .Where(p => p.Id != propertyId)
            .Take(count)
            .Select(p => MapToCard(p, new HashSet<int>()))
            .ToList();

        return ServiceResult<IReadOnlyList<PropertyCardDto>>.Success(dtos);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static PropertyCardDto MapToCard(Property p, HashSet<int> favoriteIds) => new()
    {
        PropertyId = p.Id,
        Title = p.Title,
        AreaNameEn = p.Area?.NameEn ?? string.Empty,
        UniversityNameEn = p.University?.NameEn,
        PricePerMonth = p.PricePerMonth,
        PropertyTypeDisplay = p.PropertyType.ToString(),
        AverageRating = p.AverageRating,
        PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                           ?? p.Images.FirstOrDefault()?.ImageUrl,
        IsFavorited = favoriteIds.Contains(p.Id)
    };

    private static PropertyDetailDto MapToDetail(
        Property p,
        bool isFavorited,
        bool hasActiveBooking) => new()
        {
            PropertyId = p.Id,
            OwnerId = p.OwnerId,
            OwnerFullName = p.Owner.FullName,
            OwnerImageUrl = p.Owner.ProfileImageUrl,
            OwnerIsVerified = p.Owner.IsIdentityVerified,
            UniversityId = p.UniversityId,
            UniversityNameEn = p.University?.NameEn,
            UniversityNameAr = p.University?.NameAr,
            AreaId = p.AreaId,
            AreaNameEn = p.Area.NameEn,
            AreaNameAr = p.Area.NameAr,
            Title = p.Title,
            Description = p.Description,
            PropertyType = p.PropertyType,
            SmokingAllowed = p.SmokingAllowed,
            Rooms = p.Rooms,
            Beds = p.Beds,
            PricePerMonth = p.PricePerMonth,
            Address = p.Address,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            IsAvailable = p.IsAvailable,
            IsApproved = p.IsApproved,
            AverageRating = p.AverageRating,
            CreatedAt = p.CreatedAt,
            Images = p.Images
                               .OrderByDescending(i => i.IsPrimary)
                               .Select(i => new PropertyImageDto
                               {
                                   ImageId = i.Id,
                                   ImageUrl = i.ImageUrl,
                                   IsPrimary = i.IsPrimary
                               }).ToList(),
            VideoUrls = p.Videos.Select(v => v.VideoUrl).ToList(),
            Amenities = p.PropertyAmenities
                               .Select(pa => new AmenityDto
                               {
                                   AmenityId = pa.Amenity.Id,
                                   NameEn = pa.Amenity.NameEn,
                                   NameAr = pa.Amenity.NameAr,
                                   IconClass = pa.Amenity.IconClass
                               }).ToList(),
            Reviews = p.Reviews
                               .OrderByDescending(r => r.CreatedAt)
                               .Select(r => new PropertyReviewDto
                               {
                                   ReviewId = r.Id,
                                   StudentFullName = r.Student.FullName,
                                   StudentImageUrl = r.Student.ProfileImageUrl,
                                   OverallRating = r.OverallRating,
                                   CleanlinessRating = r.CleanlinessRating,
                                   SafetyRating = r.SafetyRating,
                                   InternetRating = r.InternetRating,
                                   LocationRating = r.LocationRating,
                                   QuietnessRating = r.QuietnessRating,
                                   Comment = r.Comment,
                                   CreatedAt = r.CreatedAt
                               }).ToList(),
            IsFavorited = isFavorited,
            HasActiveBooking = hasActiveBooking
        };

    private async Task NotifyAdminPendingPropertyAsync(
        Property property,
        CancellationToken ct)
    {
        // Fetch all admin user IDs and send a notification to each
        var admins = await _uow.Repository<Admin>().GetAllAsync(ct);
        foreach (var admin in admins)
        {
            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = admin.UserId,
                Title = "New Property Pending Approval",
                Message = $"Property '{property.Title}' needs review.",
                Type = NotificationType.PropertyApproval,
                RelatedEntityId = property.Id,
                RelatedEntityType = "Property"
            }, ct);
        }
    }
}