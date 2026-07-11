using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<Booking?> GetDetailAsync(int bookingId, CancellationToken ct = default);

    Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByStudentIdAsync(
        string studentId,
        int pageNumber,
        int pageSize,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default);

    Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByPropertyIdAsync(
        int propertyId,
        int pageNumber,
        int pageSize,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetPendingByOwnerIdAsync(
        string ownerId,
        CancellationToken ct = default);

    Task<bool> IsPropertyAvailableAsync(
        int propertyId,
        DateOnly checkIn,
        DateOnly? checkOut,
        int? excludeBookingId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetConfirmedAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    Task<decimal> GetTotalCommissionAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);
}
