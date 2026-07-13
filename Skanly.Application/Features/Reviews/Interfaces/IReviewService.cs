// Skanly.Application/Features/Reviews/Interfaces/IReviewService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Reviews.DTOs;

namespace Skanly.Application.Features.Reviews.Interfaces;

public interface IReviewService
{
    // ── Student operations ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates a review. Enforces:
    /// - Booking must be Confirmed
    /// - Booking must belong to the student
    /// - No existing review for this booking (one per booking)
    /// - 30-day window from booking confirmation is recommended
    ///   but not enforced (students sometimes move in late)
    /// </summary>
    Task<ServiceResult<ReviewDto>> CreateAsync(
        string studentId,
        CreateReviewDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Edits an existing review. Enforces:
    /// - Review belongs to the student
    /// - Review was not already edited (one edit lifetime rule)
    /// - Review was created within the last 30 days
    /// - Review is not hidden by Admin
    /// </summary>
    Task<ServiceResult<ReviewDto>> UpdateAsync(
        string studentId,
        UpdateReviewDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Student deletes their own review (soft approach — recalculates average).
    /// Only allowed within 30 days and if review not hidden.
    /// </summary>
    Task<ServiceResult> DeleteAsync(
        string studentId,
        int reviewId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all reviews written by a student, paged.
    /// </summary>
    Task<ServiceResult<PagedResult<ReviewDto>>> GetByStudentAsync(
        string studentId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the review for a specific booking, or null if not yet reviewed.
    /// Used to show/hide the "Write Review" button on the bookings page.
    /// </summary>
    Task<ServiceResult<ReviewDto?>> GetByBookingAsync(
        string studentId,
        int bookingId,
        CancellationToken ct = default);

    // ── Public reads ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all visible reviews for a property with summary stats.
    /// Used on the property detail page.
    /// </summary>
    Task<ServiceResult<(IReadOnlyList<ReviewDto> Reviews,
                        ReviewSummaryDto Summary)>> GetByPropertyAsync(
        int propertyId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the aggregated rating summary for a property.
    /// </summary>
    Task<ServiceResult<ReviewSummaryDto>> GetSummaryAsync(
        int propertyId,
        CancellationToken ct = default);

    // ── Admin operations ───────────────────────────────────────────────────────

    Task<ServiceResult<PagedResult<ReviewDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? searchTerm = null,
        bool? hiddenOnly = null,
        CancellationToken ct = default);

    Task<ServiceResult> HideAsync(
        int reviewId,
        CancellationToken ct = default);

    Task<ServiceResult> UnhideAsync(
        int reviewId,
        CancellationToken ct = default);

    /// <summary>
    /// Admin hard-delete (only for illegal/abusive content).
    /// Recalculates property average after deletion.
    /// </summary>
    Task<ServiceResult> AdminDeleteAsync(
        int reviewId,
        CancellationToken ct = default);
}