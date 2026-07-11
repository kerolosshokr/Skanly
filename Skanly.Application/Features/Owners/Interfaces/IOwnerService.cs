using Microsoft.AspNetCore.Http;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Application.Features.Owners.DTOs;

namespace Skanly.Application.Features.Owners.Interfaces;

public interface IOwnerService
{
    Task<ServiceResult<OwnerProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken ct = default);

    Task<ServiceResult<OwnerProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateOwnerProfileDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<string>> UploadProfileImageAsync(
        string userId,
        IFormFile image,
        CancellationToken ct = default);

    Task<ServiceResult> SubmitIdentityVerificationAsync(
        string userId,
        UploadIdentityDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<OwnerDashboardDto>> GetDashboardAsync(
        string userId,
        CancellationToken ct = default);

    Task<ServiceResult<PagedResult<OwnerPropertySummaryDto>>> GetPropertiesAsync(
        string ownerId,
        int pageNumber = 1,
        int pageSize = 10,
        string? statusFilter = null,
        CancellationToken ct = default);

    Task<ServiceResult<PagedResult<BookingRequestDto>>> GetBookingRequestsAsync(
        string ownerId,
        int pageNumber = 1,
        int pageSize = 10,
        string? statusFilter = null,
        CancellationToken ct = default);

    Task<ServiceResult<BookingRequestDto>> GetBookingRequestDetailAsync(
        string ownerId,
        int bookingId,
        CancellationToken ct = default);

    Task<ServiceResult> HandleBookingRequestAsync(
        string ownerId,
        HandleBookingRequestDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<EarningsSummaryDto>> GetEarningsAsync(
        string ownerId,
        int year,
        CancellationToken ct = default);

    Task<ServiceResult<PagedResult<NotificationDto>>> GetNotificationsAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ServiceResult> MarkNotificationReadAsync(
        string userId,
        long notificationId,
        CancellationToken ct = default);

    Task<ServiceResult> MarkAllNotificationsReadAsync(
        string userId,
        CancellationToken ct = default);

    Task<ServiceResult<int>> GetUnreadNotificationCountAsync(
        string userId,
        CancellationToken ct = default);
}
