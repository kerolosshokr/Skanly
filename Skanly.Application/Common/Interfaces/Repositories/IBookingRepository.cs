// Skanly.Application/Common/Interfaces/Repositories/IBookingRepository.cs
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    /// <summary>Returns a booking with all navigation data (Student, Property, Owner, Payments, Contract).</summary>
    Task<Booking?> GetDetailAsync(int bookingId, CancellationToken ct = default);

    /// <summary>Returns all bookings for a student, paged.</summary>
    Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByStudentIdAsync(
        string studentId,
        int pageNumber,
        int pageSize,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default);

    /// <summary>Returns all bookings for a property (used by Owner dashboard).</summary>
    Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByPropertyIdAsync(
        int propertyId,
        int pageNumber,
        int pageSize,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default);

    /// <summary>Returns all pending bookings across all owner properties (Owner review queue).</summary>
    Task<IReadOnlyList<Booking>> GetPendingByOwnerIdAsync(
        string ownerId,
        CancellationToken ct = default);

    /// <summary>Checks if a property is available for a given date range (no confirmed/accepted booking).</summary>
    Task<bool> IsPropertyAvailableAsync(
        int propertyId,
        DateOnly checkIn,
        DateOnly? checkOut,
        int? excludeBookingId = null,
        CancellationToken ct = default);

    /// <summary>Returns confirmed bookings for analytics / revenue calculation.</summary>
    Task<IReadOnlyList<Booking>> GetConfirmedAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>Returns total commission earned in a date range (Admin analytics).</summary>
    Task<decimal> GetTotalCommissionAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);
}