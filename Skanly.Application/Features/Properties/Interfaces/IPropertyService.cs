using Skanly.Application.Common.Models;
using Skanly.Application.Features.Properties.DTOs;

namespace Skanly.Application.Features.Properties.Interfaces;

public interface IPropertyService
{
    Task<ServiceResult<PagedResult<PropertyCardDto>>> SearchAsync(
        PropertySearchRequestDto request,
        string? viewerUserId = null,
        CancellationToken ct = default);

    Task<ServiceResult<PropertyDetailDto>> GetDetailAsync(
        int propertyId,
        string? viewerUserId = null,
        CancellationToken ct = default);

    Task<ServiceResult<PropertyDetailDto>> CreateAsync(
        string ownerId,
        CreatePropertyDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<PropertyDetailDto>> UpdateAsync(
        string ownerId,
        UpdatePropertyDto dto,
        CancellationToken ct = default);

    Task<ServiceResult> SoftDeleteAsync(
        string ownerId,
        int propertyId,
        CancellationToken ct = default);

    Task<ServiceResult> SetPrimaryImageAsync(
        string ownerId,
        int propertyId,
        int imageId,
        CancellationToken ct = default);

    Task<ServiceResult> DeleteImageAsync(
        string ownerId,
        int propertyId,
        int imageId,
        CancellationToken ct = default);

    Task<ServiceResult> ToggleAvailabilityAsync(
        string ownerId,
        int propertyId,
        CancellationToken ct = default);

    Task<ServiceResult<PagedResult<PropertyCardDto>>> GetPendingApprovalAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<ServiceResult> ApproveAsync(
        int propertyId,
        CancellationToken ct = default);

    Task<ServiceResult> RejectAsync(
        int propertyId,
        string reason,
        CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<AmenityDto>>> GetAllAmenitiesAsync(
        CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<PropertyCardDto>>> GetRelatedAsync(
        int propertyId,
        int count = 4,
        CancellationToken ct = default);
}
