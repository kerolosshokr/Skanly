using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Skanly.Infrastructure.RealTime;
using System.Security.Claims;

namespace Skanly.Infrastructure.RealTime.Hubs
{

    /// <summary>
    /// Lightweight hub for real-time notification delivery.
    /// Does not handle message exchange — only pushes notification
    /// events from server to client.
    ///
    /// Client methods:
    ///   ReceiveNotification(NotificationDto)
    ///   NotificationRead(long notificationId)
    ///   UnreadCountUpdated(int count)
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ConnectionTracker _tracker;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(
            ConnectionTracker tracker,
            ILogger<NotificationHub> logger)
        {
            _tracker = tracker;
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _tracker.Add(userId, Context.ConnectionId);

            _logger.LogDebug(
                "Notification hub: user {UserId} connected", userId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _tracker.Remove(userId, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        private string GetUserId()
            => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new HubException("Unauthenticated.");
    }
}
