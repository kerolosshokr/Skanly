// Skanly.Application/Features/Notifications/Services/NotificationService.cs
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;


namespace Skanly.Application.Features.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationHub _hubHelper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
      IUnitOfWork uow,
      INotificationHub hubHelper,
      ILogger<NotificationService> logger)
    {
        _uow = uow;
        _hubHelper = hubHelper;
        _logger = logger;
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<NotificationDto>>> GetPagedAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        NotificationType? typeFilter = null,
        bool? unreadOnly = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Notifications.GetByUserIdAsync(
            userId, pageNumber, pageSize, ct);

        // Apply in-memory filters (small data sets per user)
        var filtered = items
            .Where(n => typeFilter == null || n.Type == typeFilter)
            .Where(n => unreadOnly == null ||
                        (unreadOnly.Value ? !n.IsRead : n.IsRead))
            .ToList();

        var dtos = filtered.Select(MapToDto).ToList();

        return ServiceResult<PagedResult<NotificationDto>>.Success(
            PagedResult<NotificationDto>.Create(
                dtos, total, pageNumber, pageSize));
    }

    // ── GetRecentAsync ────────────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<NotificationDto>>> GetRecentAsync(
        string userId,
        int count = 8,
        CancellationToken ct = default)
    {
        var (items, _) = await _uow.Notifications
            .GetByUserIdAsync(userId, 1, count, ct);

        var dtos = items.Select(MapToDto).ToList();

        return ServiceResult<IReadOnlyList<NotificationDto>>.Success(dtos);
    }

    // ── GetUnreadCountAsync ───────────────────────────────────────────────────

    public async Task<ServiceResult<int>> GetUnreadCountAsync(
        string userId,
        CancellationToken ct = default)
    {
        var count = await _uow.Notifications.GetUnreadCountAsync(userId, ct);
        return ServiceResult<int>.Success(count);
    }

    // ── MarkReadAsync ─────────────────────────────────────────────────────────

    public async Task<ServiceResult> MarkReadAsync(
        string userId,
        long notificationId,
        CancellationToken ct = default)
    {
        // Guard: notification must belong to this user
        var notification = await _uow.Repository<Notification>()
            .GetFirstOrDefaultAsync(
                n => n.NotificationId == notificationId &&
                     n.UserId == userId, ct);

        if (notification is null)
            return ServiceResult.Failure("Notification not found.");

        await _uow.Notifications.MarkAsReadAsync(notificationId, ct);

        // Push updated count to client
        await PushUpdatedCountAsync(userId, ct);

        return ServiceResult.Success();
    }

    // ── MarkAllReadAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult> MarkAllReadAsync(
        string userId,
        CancellationToken ct = default)
    {
        await _uow.Notifications.MarkAllAsReadAsync(userId, ct);
        await PushUpdatedCountAsync(userId, ct);
        return ServiceResult.Success();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // TYPED NOTIFICATION SENDERS
    // Each method:
    //   1. Builds the Notification entity
    //   2. Persists to DB (guarantee of delivery even if user is offline)
    //   3. Pushes real-time via NotificationHub if user is online
    // ══════════════════════════════════════════════════════════════════════════

    // ── Booking Notifications ─────────────────────────────────────────────────

    public async Task SendBookingReceivedAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        string studentName,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: ownerId,
            title: "New Booking Request",
            message: $"{studentName} has requested to book " +
                                $"\"{propertyTitle}\". Review and respond.",
            type: NotificationType.BookingUpdate,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

    public async Task SendBookingAcceptedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: studentId,
            title: "Booking Accepted! 🎉",
            message: $"The owner accepted your booking request for " +
                                $"\"{propertyTitle}\". Proceed to payment to confirm.",
            type: NotificationType.BookingUpdate,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

    public async Task SendBookingRejectedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        string? reason = null,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: studentId,
            title: "Booking Request Declined",
            message: string.IsNullOrEmpty(reason)
                ? $"Your booking request for \"{propertyTitle}\" was declined. " +
                  "You can search for other available properties."
                : $"Your booking request for \"{propertyTitle}\" was declined. " +
                  $"Reason: {reason}",
            type: NotificationType.BookingUpdate,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

    public async Task SendBookingConfirmedAsync(
        string studentId,
        string ownerId,
        int bookingId,
        string propertyTitle,
        CancellationToken ct = default)
    {
        // Notify student
        await SendAndPushAsync(
            userId: studentId,
            title: "Booking Confirmed! ✅",
            message: $"Your booking for \"{propertyTitle}\" is confirmed. " +
                                "Your contract has been generated.",
            type: NotificationType.BookingUpdate,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

        // Notify owner
        await SendAndPushAsync(
            userId: ownerId,
            title: "Booking Confirmed — Payment Received",
            message: $"A student has completed payment for " +
                                $"\"{propertyTitle}\". Booking #{bookingId} is confirmed.",
            type: NotificationType.BookingUpdate,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);
    }

    public async Task SendBookingCancelledAsync(
        string recipientId,
        int bookingId,
        string propertyTitle,
        string cancelledBy,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: recipientId,
            title: "Booking Cancelled",
            message: $"The booking for \"{propertyTitle}\" " +
                                $"has been cancelled by {cancelledBy}.",
            type: NotificationType.BookingUpdate,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

    // ── Payment Notifications ─────────────────────────────────────────────────

    public async Task SendPaymentSuccessAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        decimal amount,
        string transactionRef,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: studentId,
            title: "Payment Successful 💳",
            message: $"EGP {amount:N0} received for \"{propertyTitle}\". " +
                                $"Reference: {transactionRef}. " +
                                "Your booking is now confirmed.",
            type: NotificationType.PaymentConfirmation,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

    public async Task SendPaymentFailedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        string reason,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: studentId,
            title: "Payment Failed",
            message: $"Your payment for \"{propertyTitle}\" failed. " +
                                $"Reason: {reason}. Please try again.",
            type: NotificationType.PaymentConfirmation,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

    public async Task SendOwnerPayoutNoticeAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        decimal netAmount,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: ownerId,
            title: "Earnings Update 💰",
            message: $"EGP {netAmount:N0} earned from booking #{bookingId} " +
                                $"for \"{propertyTitle}\" (after platform commission).",
            type: NotificationType.PaymentConfirmation,
            relatedEntityId: bookingId,
            relatedEntityType: "Booking",
            ct: ct);

    // ── Chat Notifications ────────────────────────────────────────────────────

    public async Task SendNewMessageAsync(
        string recipientId,
        int conversationId,
        string senderName,
        string messagePreview,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: recipientId,
            title: $"New message from {senderName}",
            message: messagePreview.Length > 80
                ? messagePreview[..80] + "…"
                : messagePreview,
            type: NotificationType.NewMessage,
            relatedEntityId: conversationId,
            relatedEntityType: "Conversation",
            ct: ct);

    // ── Property Notifications ────────────────────────────────────────────────

    public async Task SendPropertyApprovedAsync(
        string ownerId,
        int propertyId,
        string propertyTitle,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: ownerId,
            title: "Property Listing Approved! 🏠",
            message: $"Your property \"{propertyTitle}\" has been " +
                                "reviewed and approved. It is now live and " +
                                "visible to students.",
            type: NotificationType.PropertyApproval,
            relatedEntityId: propertyId,
            relatedEntityType: "Property",
            ct: ct);

    public async Task SendPropertyRejectedAsync(
        string ownerId,
        int propertyId,
        string propertyTitle,
        string reason,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: ownerId,
            title: "Property Listing Not Approved",
            message: $"Your listing \"{propertyTitle}\" was not approved. " +
                                $"Reason: {reason}. Please update and resubmit.",
            type: NotificationType.PropertyApproval,
            relatedEntityId: propertyId,
            relatedEntityType: "Property",
            ct: ct);

    // ── Verification Notifications ────────────────────────────────────────────

    public async Task SendVerificationApprovedAsync(
        string userId,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: userId,
            title: "Identity Verified! 🛡️",
            message: "Your identity has been verified successfully. " +
                                "You now have full access to all Skanly features.",
            type: NotificationType.VerificationApproval,
            relatedEntityId: null,
            relatedEntityType: "Verification",
            ct: ct);

    public async Task SendVerificationRejectedAsync(
        string userId,
        string reason,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: userId,
            title: "Identity Verification Failed",
            message: $"Your identity verification was not approved. " +
                                $"Reason: {reason}. Please resubmit with clearer documents.",
            type: NotificationType.VerificationApproval,
            relatedEntityId: null,
            relatedEntityType: "Verification",
            ct: ct);

    // ── Report Notifications ──────────────────────────────────────────────────

    public async Task SendReportStatusUpdateAsync(
        string reporterId,
        int reportId,
        string newStatus,
        string resolution,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId: reporterId,
            title: $"Report Update — {newStatus}",
            message: $"Your report #{reportId} has been {newStatus.ToLower()}. " +
                                $"Notes: {resolution}",
            type: NotificationType.BookingUpdate,
            relatedEntityId: reportId,
            relatedEntityType: "Report",
            ct: ct);

    // ── Generic Sender ────────────────────────────────────────────────────────

    public async Task SendAsync(
        string userId,
        string title,
        string message,
        NotificationType type,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default)
        => await SendAndPushAsync(
            userId, title, message, type,
            relatedEntityId, relatedEntityType, ct);

    // ── Core Private Method ───────────────────────────────────────────────────

    /// <summary>
    /// Persist to DB then push real-time.
    /// DB write always happens — real-time push is best-effort.
    /// </summary>
    private async Task SendAndPushAsync(
        string userId,
        string title,
        string message,
        NotificationType type,
        int? relatedEntityId,
        string? relatedEntityType,
        CancellationToken ct)
    {
        // 1. Persist (offline delivery guarantee)
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Notifications.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Notification persisted for User={UserId} Type={Type} " +
            "Title={Title}",
            userId, type, title);

        // 2. Real-time push (best-effort — never throws)
        try
        {
            var dto = MapToDto(notification);
            await _hubHelper.PushNotificationAsync(userId, dto, ct);

            // Update unread count badge
            var count = await _uow.Notifications
                .GetUnreadCountAsync(userId, ct);
            await _hubHelper.PushUnreadCountAsync(userId, count, ct);
        }
        catch (Exception ex)
        {
            // Hub errors must never fail the calling service
            _logger.LogWarning(ex,
                "Real-time push failed for User={UserId}. " +
                "DB record is safe.", userId);
        }
    }

    // ── Count Badge Helper ────────────────────────────────────────────────────

    private async Task PushUpdatedCountAsync(
        string userId,
        CancellationToken ct)
    {
        try
        {
            var count = await _uow.Notifications
                .GetUnreadCountAsync(userId, ct);
            await _hubHelper.PushUnreadCountAsync(userId, count, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to push updated count for User={UserId}", userId);
        }
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static NotificationDto MapToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        UserId = n.UserId,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        IsRead = n.IsRead,
        RelatedEntityId = n.RelatedEntityId,
        RelatedEntityType = n.RelatedEntityType,
        CreatedAt = n.CreatedAt
    };
}