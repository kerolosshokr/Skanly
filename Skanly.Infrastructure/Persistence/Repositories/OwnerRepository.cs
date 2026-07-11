using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class OwnerRepository : GenericRepository<Owner>, IOwnerRepository
{
    public OwnerRepository(SkanlyDbContext context) : base(context) { }

    public async Task<Owner?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(o => o.UserId == userId, ct);

    public async Task<Owner?> GetWithPropertiesAsync(string userId, CancellationToken ct = default)
        => await _dbSet
            .Include(o => o.Properties).ThenInclude(p => p.Area)
            .Include(o => o.Properties).ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .FirstOrDefaultAsync(o => o.UserId == userId, ct);

    public async Task<decimal> GetTotalEarningsAsync(string ownerId, CancellationToken ct = default)
        => await _context.Bookings
            .Where(b =>
                b.Property.OwnerId == ownerId &&
                b.Status == BookingStatus.Confirmed)
            .SumAsync(b => b.TotalAmount - (b.CommissionAmount ?? 0), ct);

    public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
        => await _dbSet.CountAsync(ct);
}