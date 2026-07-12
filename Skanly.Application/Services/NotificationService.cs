using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Application.Interfaces.Services;

namespace Skanly.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork uow,
        ILogger<NotificationService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task SendBookingReceivedAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        string studentName,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Booking received. Owner:{OwnerId}, Booking:{BookingId}",
            ownerId,
            bookingId);

        await Task.CompletedTask;
    }

    public async Task SendBookingCancelledAsync(
        string recipientId,
        int bookingId,
        string propertyTitle,
        string cancelledBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Booking cancelled. Recipient:{RecipientId}, Booking:{BookingId}",
            recipientId,
            bookingId);

        await Task.CompletedTask;
    }

    public async Task SendBookingAcceptedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Booking accepted. Student:{StudentId}, Booking:{BookingId}",
            studentId,
            bookingId);

        await Task.CompletedTask;
    }

    public async Task SendBookingRejectedAsync(
        string studentId,
        int bookingId,
        string propertyTitle,
        string reason,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Booking rejected. Student:{StudentId}, Booking:{BookingId}",
            studentId,
            bookingId);

        await Task.CompletedTask;
    }

    public async Task SendBookingConfirmedAsync(
        string studentId,
        string ownerId,
        int bookingId,
        string propertyTitle,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Booking confirmed. Booking:{BookingId}",
            bookingId);

        await Task.CompletedTask;
    }

    public async Task SendOwnerPayoutNoticeAsync(
        string ownerId,
        int bookingId,
        string propertyTitle,
        decimal netAmount,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Owner payout notice. Owner:{OwnerId}, Booking:{BookingId}, Amount:{Amount}",
            ownerId,
            bookingId,
            netAmount);

        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
        string userId,
        CancellationToken ct = default)
    {
        return await Task.FromResult<IReadOnlyList<NotificationDto>>(new List<NotificationDto>());
    }

    public async Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken ct = default)
    {
        return await Task.FromResult(0);
    }

    public async Task MarkAsReadAsync(
        int notificationId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }

    public async Task MarkAllAsReadAsync(
        string userId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }
}