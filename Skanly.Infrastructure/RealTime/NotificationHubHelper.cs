// Skanly.Infrastructure/RealTime/NotificationHubHelper.cs
using Microsoft.AspNetCore.SignalR;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Infrastructure.RealTime.Hubs;

namespace Skanly.Infrastructure.RealTime;

/// <summary>
/// Injects IHubContext so Application-layer services can push
/// real-time events without depending on SignalR directly.
/// Registered as Scoped in Infrastructure DI.
/// </summary>
public class NotificationHubHelper
{
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly ConnectionTracker _tracker;

    public NotificationHubHelper(
        IHubContext<NotificationHub> notificationHub,
        IHubContext<ChatHub> chatHub,
        ConnectionTracker tracker)
    {
        _notificationHub = notificationHub;
        _chatHub = chatHub;
        _tracker = tracker;
    }

    /// <summary>
    /// Push a notification to a user in real-time if they are online.
    /// If offline, the DB record alone serves as the delivery mechanism
    /// (polled on next login).
    /// </summary>
    public async Task PushNotificationAsync(
        string userId,
        NotificationDto notification,
        CancellationToken ct = default)
    {
        var connections = _tracker.GetConnectionIds(userId);
        if (!connections.Any()) return;

        await _notificationHub.Clients
            .Clients(connections)
            .SendAsync("ReceiveNotification", notification, ct);
    }

    /// <summary>
    /// Update the unread count badge for a user in real time.
    /// </summary>
    public async Task PushUnreadCountAsync(
        string userId,
        int count,
        CancellationToken ct = default)
    {
        var connections = _tracker.GetConnectionIds(userId);
        if (!connections.Any()) return;

        await _notificationHub.Clients
            .Clients(connections)
            .SendAsync("UnreadCountUpdated", count, ct);
    }

    public bool IsOnline(string userId)
        => _tracker.IsOnline(userId);

    public int GetOnlineUserCount()
        => _tracker.GetOnlineCount();
}