using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);

    Task MarkAsReadAsync(long notificationId, CancellationToken ct = default);
}
