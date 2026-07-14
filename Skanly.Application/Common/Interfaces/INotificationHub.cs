using Skanly.Application.Features.Notifications.DTOs;

namespace Skanly.Application.Common.Interfaces;

public interface INotificationHub
{
    Task PushNotificationAsync(
        string userId,
        NotificationDto notification,
        CancellationToken cancellationToken = default);

    Task PushUnreadCountAsync(
        string userId,
        int unreadCount,
        CancellationToken cancellationToken = default);
}