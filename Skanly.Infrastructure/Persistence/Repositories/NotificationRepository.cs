using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(SkanlyDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking().Where(n => n.UserId == userId);
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        => await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
        => await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(n => n.IsRead, true), ct);

    public async Task MarkAsReadAsync(long notificationId, CancellationToken ct = default)
        => await _dbSet
            .Where(n => n.NotificationId == notificationId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(n => n.IsRead, true), ct);
}