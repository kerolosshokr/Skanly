// Skanly.Application/Features/Payments/Interfaces/IPaymentService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Payments.DTOs;

namespace Skanly.Application.Features.Payments.Interfaces;

public interface IPaymentService
{
    // ── Student operations ─────────────────────────────────────────────────────

    /// <summary>
    /// Assembles all data needed to render the checkout page.
    /// Validates the booking belongs to the student and is
    /// in PaymentPending or Accepted state.
    /// </summary>
    Task<ServiceResult<CheckoutViewModel>> GetCheckoutAsync(
        string studentId,
        int bookingId,
        CancellationToken ct = default);

    /// <summary>
    /// Processes a payment. On success: booking → Confirmed,
    /// contract generation triggered, notifications sent.
    /// On failure: payment recorded as Failed, booking stays in
    /// Accepted / PaymentPending.
    /// </summary>
    Task<ServiceResult<PaymentResultDto>> ProcessPaymentAsync(
        string studentId,
        InitiatePaymentDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all payments for a student (sorted newest first).
    /// </summary>
    Task<ServiceResult<PagedResult<PaymentDto>>> GetStudentPaymentHistoryAsync(
        string studentId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single payment by ID, guarded to the student.
    /// </summary>
    Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(
        string studentId,
        int paymentId,
        CancellationToken ct = default);

    // ── Admin operations ───────────────────────────────────────────────────────

    Task<ServiceResult<PagedResult<PaymentHistoryDto>>> GetAllPaymentsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? statusFilter = null,
        string? methodFilter = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    Task<ServiceResult<PaymentSummaryDto>> GetPaymentSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);
}