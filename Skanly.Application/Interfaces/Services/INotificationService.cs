using Skanly.Application.Features.Notifications.DTOs;

namespace Skanly.Application.Interfaces.Services;

public interface INotificationService
{
    Task SendBookingReceivedAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        string studentName,
        CancellationToken ct = default);

    Task SendBookingCancelledAsync(
        string recipientId,
        int bookingId,
        string propertyTitle,
        string cancelledBy,
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
        string reason,
        CancellationToken ct = default);

    Task SendBookingConfirmedAsync(
        string studentId,
        string ownerId,
        int bookingId,
        string propertyTitle,
        CancellationToken ct = default);

    Task SendOwnerPayoutNoticeAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        decimal netAmount,
        CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
        string userId,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken ct = default);

    Task MarkAsReadAsync(
        int notificationId,
        CancellationToken ct = default);

    Task MarkAllAsReadAsync(
        string userId,
        CancellationToken ct = default);
}