// Skanly.Application/Features/Students/Interfaces/IStudentService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Bookings.DTOs;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Application.Features.Students.DTOs;
using Microsoft.AspNetCore.Http;

namespace Skanly.Application.Features.Students.Interfaces;

public interface IStudentService
{
    // ── Profile ───────────────────────────────────────────────────────────────
    Task<ServiceResult<StudentProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken ct = default);

    Task<ServiceResult<StudentProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateProfileDto dto,
        CancellationToken ct = default);

    Task<ServiceResult> CompleteProfileAsync(
        string userId,
        CompleteProfileDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<string>> UploadProfileImageAsync(
        string userId,
        IFormFile image,
        CancellationToken ct = default);

    // ── Identity Verification ─────────────────────────────────────────────────
    Task<ServiceResult> SubmitIdentityVerificationAsync(
        string userId,
        UploadIdentityDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<string>> GetVerificationStatusAsync(
        string userId,
        CancellationToken ct = default);

    // ── Dashboard ─────────────────────────────────────────────────────────────
    Task<ServiceResult<StudentDashboardDto>> GetDashboardAsync(
        string userId,
        CancellationToken ct = default);

    // ── Bookings (read — write ops in BookingService) ─────────────────────────
    Task<ServiceResult<PagedResult<BookingSummaryDto>>> GetBookingsAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 10,
        string? statusFilter = null,
        CancellationToken ct = default);

    // ── Favorites (read — write ops in FavoriteService) ──────────────────────
    Task<ServiceResult<PagedResult<PropertyCardDto>>> GetFavoritesAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 12,
        CancellationToken ct = default);

    // ── Notifications ─────────────────────────────────────────────────────────
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