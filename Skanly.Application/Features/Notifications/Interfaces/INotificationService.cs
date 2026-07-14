// Skanly.Application/Features/Notifications/Interfaces/INotificationService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Notifications.Interfaces;

public interface INotificationService
{
    // ── Queries ────────────────────────────────────────────────────────────────

    Task<ServiceResult<PagedResult<NotificationDto>>> GetPagedAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        NotificationType? typeFilter = null,
        bool? unreadOnly = null,
        CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<NotificationDto>>> GetRecentAsync(
        string userId,
        int count = 8,
        CancellationToken ct = default);

    Task<ServiceResult<int>> GetUnreadCountAsync(
        string userId,
        CancellationToken ct = default);

    // ── Mark read ──────────────────────────────────────────────────────────────

    Task<ServiceResult> MarkReadAsync(
        string userId,
        long notificationId,
        CancellationToken ct = default);

    Task<ServiceResult> MarkAllReadAsync(
        string userId,
        CancellationToken ct = default);

    // ── Typed notification senders ─────────────────────────────────────────────
    // Each method persists to DB + pushes real-time if recipient is online.

    // Booking
    Task SendBookingReceivedAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        string studentName,
        CancellationToken ct = default);

    Task SendBookingAcceptedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        CancellationToken ct = default);

    Task SendBookingRejectedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        string? reason = null,
        CancellationToken ct = default);

    Task SendBookingConfirmedAsync(
        string studentId,
        string ownerId,
        int bookingId,
        string propertyTitle,
        CancellationToken ct = default);

    Task SendBookingCancelledAsync(
        string recipientId,
        int bookingId,
        string propertyTitle,
        string cancelledBy,
        CancellationToken ct = default);

    // Payment
    Task SendPaymentSuccessAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        decimal amount,
        string transactionRef,
        CancellationToken ct = default);

    Task SendPaymentFailedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        string reason,
        CancellationToken ct = default);

    Task SendOwnerPayoutNoticeAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        decimal netAmount,
        CancellationToken ct = default);

    // Chat
    Task SendNewMessageAsync(
        string recipientId,
        int conversationId,
        string senderName,
        string messagePreview,
        CancellationToken ct = default);

    // Property
    Task SendPropertyApprovedAsync(
        string ownerId,
        int propertyId,
        string propertyTitle,
        CancellationToken ct = default);

    Task SendPropertyRejectedAsync(
        string ownerId,
        int propertyId,
        string propertyTitle,
        string reason,
        CancellationToken ct = default);

    // Identity Verification
    Task SendVerificationApprovedAsync(
        string userId,
        CancellationToken ct = default);

    Task SendVerificationRejectedAsync(
        string userId,
        string reason,
        CancellationToken ct = default);

    // Reports
    Task SendReportStatusUpdateAsync(
        string reporterId,
        int reportId,
        string newStatus,
        string resolution,
        CancellationToken ct = default);

    // Generic (internal use or for parts that don't have a typed method yet)
    Task SendAsync(
        string userId,
        string title,
        string message,
        NotificationType type,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default);
}