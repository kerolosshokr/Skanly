// Skanly.Application/Features/Bookings/Interfaces/IBookingService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Bookings.DTOs;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Bookings.Interfaces;

public interface IBookingService
{
    // ── Student operations ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates a booking request.
    /// Business rules:
    /// - Student must be identity verified
    /// - Property must be approved and available
    /// - No overlapping confirmed/accepted booking for that property
    /// - Student cannot have an open booking for the same property
    /// </summary>
    Task<ServiceResult<BookingDto>> CreateAsync(
        string studentId,
        CreateBookingDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a booking. Only Pending or Accepted bookings can be cancelled.
    /// Notifies owner if booking was Accepted.
    /// </summary>
    Task<ServiceResult> CancelAsync(
        string studentId,
        CancelBookingDto dto,
        CancellationToken ct = default);

    /// <summary>Returns paged bookings for a student with optional status filter.</summary>
    Task<ServiceResult<PagedResult<BookingDto>>> GetByStudentAsync(
        string studentId,
        int pageNumber = 1,
        int pageSize = 10,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default);

    /// <summary>Returns full booking detail for a student.</summary>
    Task<ServiceResult<BookingDetailDto>> GetDetailForStudentAsync(
        string studentId,
        int bookingId,
        CancellationToken ct = default);

    // ── Owner operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Owner accepts a booking request.
    /// Transitions: Pending → Accepted.
    /// Snapshots active commission rate.
    /// Notifies student to proceed with payment.
    /// </summary>
    Task<ServiceResult> AcceptAsync(
        string ownerId,
        int bookingId,
        CancellationToken ct = default);

    /// <summary>
    /// Owner rejects a booking request.
    /// Transitions: Pending → Rejected.
    /// Notifies student with rejection reason.
    /// </summary>
    Task<ServiceResult> RejectAsync(
        string ownerId,
        int bookingId,
        string reason,
        CancellationToken ct = default);

    /// <summary>Returns paged bookings for an owner across all their properties.</summary>
    Task<ServiceResult<PagedResult<BookingDto>>> GetByOwnerAsync(
        string ownerId,
        int pageNumber = 1,
        int pageSize = 10,
        BookingStatus? statusFilter = null,
        CancellationToken ct = default);

    /// <summary>Returns full booking detail for an owner.</summary>
    Task<ServiceResult<BookingDetailDto>> GetDetailForOwnerAsync(
        string ownerId,
        int bookingId,
        CancellationToken ct = default);

    // ── System operations (called by PaymentService after payment) ────────────

    /// <summary>
    /// Confirms a booking after successful payment.
    /// Transitions: PaymentPending → Confirmed.
    /// Calculates commission, triggers contract generation,
    /// notifies student and owner.
    /// </summary>
    Task<ServiceResult> ConfirmAfterPaymentAsync(
        int bookingId,
        string transactionReference,
        CancellationToken ct = default);

    /// <summary>
    /// Moves an Accepted booking to PaymentPending state.
    /// Called when student initiates payment checkout.
    /// </summary>
    Task<ServiceResult> MarkPaymentPendingAsync(
        string studentId,
        int bookingId,
        CancellationToken ct = default);

    // ── Admin operations ───────────────────────────────────────────────────────

    Task<ServiceResult<PagedResult<BookingDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        BookingStatus? statusFilter = null,
        string? searchTerm = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    Task<ServiceResult<BookingDetailDto>> GetDetailForAdminAsync(
        int bookingId,
        CancellationToken ct = default);

    Task<ServiceResult<BookingStatsDto>> GetStatsAsync(
        CancellationToken ct = default);
}