using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(SkanlyDbContext context) : base(context) { }

    public async Task<Booking?> GetDetailAsync(int bookingId, CancellationToken ct = default)
        => await _dbSet
            .Include(b => b.Student)
            .Include(b => b.Property).ThenInclude(p => p.Owner)
            .Include(b => b.Property).ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .Include(b => b.Payments)
            .Include(b => b.Contract)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByStudentIdAsync(
        string studentId,
        int pageNumber,
        int pageSize,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(b => b.StudentId == studentId);

        if (statusFilter.HasValue)
            query = query.Where(b => b.Status == statusFilter);

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(b => b.Property).ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .Include(b => b.Property).ThenInclude(p => p.Area)
            .OrderByDescending(b => b.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByPropertyIdAsync(
        int propertyId,
        int pageNumber,
        int pageSize,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(b => b.PropertyId == propertyId);

        if (statusFilter.HasValue)
            query = query.Where(b => b.Status == statusFilter);

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(b => b.Student)
            .OrderByDescending(b => b.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Booking>> GetPendingByOwnerIdAsync(
        string ownerId,
        CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(b => b.Property.OwnerId == ownerId && b.Status == BookingStatus.Pending)
            .Include(b => b.Student)
            .Include(b => b.Property).ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .OrderBy(b => b.RequestedAt)
            .ToListAsync(ct);

    public async Task<bool> IsPropertyAvailableAsync(
        int propertyId,
        DateOnly checkIn,
        DateOnly? checkOut,
        int? excludeBookingId = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.Where(b =>
            b.PropertyId == propertyId &&
            (b.Status == BookingStatus.Accepted ||
             b.Status == BookingStatus.PaymentPending ||
             b.Status == BookingStatus.Confirmed) &&
            b.CheckInDate <= (checkOut ?? checkIn) &&
            (b.CheckOutDate == null || b.CheckOutDate >= checkIn));

        if (excludeBookingId.HasValue)
            query = query.Where(b => b.Id != excludeBookingId);

        return !await query.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Booking>> GetConfirmedAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Confirmed &&
                        b.CreatedAt >= from && b.CreatedAt <= to)
            .Include(b => b.Property)
            .ToListAsync(ct);

    public async Task<decimal> GetTotalCommissionAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
        => await _dbSet
            .Where(b => b.Status == BookingStatus.Confirmed &&
                        b.CreatedAt >= from && b.CreatedAt <= to)
            .SumAsync(b => b.CommissionAmount ?? 0, ct);
}