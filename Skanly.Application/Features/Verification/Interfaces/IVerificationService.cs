// Skanly.Application/Features/Verification/Interfaces/IVerificationService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Verification.DTOs;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Verification.Interfaces;

public interface IVerificationService
{
    // ── User (Student / Owner) ─────────────────────────────────────────────────

    /// <summary>
    /// Submits identity documents. Runs OCR automatically.
    /// Returns the verification record with OCR results for user confirmation.
    /// </summary>
    Task<ServiceResult<VerificationDto>> SubmitAsync(
        string userId,
        SubmitVerificationDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the latest verification record for the user.
    /// Used to display current status on profile page.
    /// </summary>
    Task<ServiceResult<VerificationDto?>> GetLatestForUserAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all verification submissions by the user (history).
    /// </summary>
    Task<ServiceResult<IReadOnlyList<VerificationDto>>> GetHistoryAsync(
        string userId,
        CancellationToken ct = default);

    // ── Admin ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all pending verifications for Admin review queue.
    /// </summary>
    Task<ServiceResult<PagedResult<VerificationDto>>> GetPendingAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all verifications with optional filters.
    /// </summary>
    Task<ServiceResult<PagedResult<VerificationDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        VerificationStatus? statusFilter = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single verification record for Admin detail view.
    /// </summary>
    Task<ServiceResult<VerificationDto>> GetByIdAsync(
        int verificationId,
        CancellationToken ct = default);

    /// <summary>
    /// Admin approves or rejects a verification.
    /// On approval: updates user profile fields + marks IsIdentityVerified.
    /// On rejection: notifies user with reason.
    /// </summary>
    Task<ServiceResult> ReviewAsync(
        string adminId,
        ReviewVerificationDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Returns dashboard summary stats for Admin.
    /// </summary>
    Task<ServiceResult<VerificationSummaryDto>> GetSummaryAsync(
        CancellationToken ct = default);
}

public class VerificationSummaryDto
{
    public int TotalPending { get; init; }
    public int TotalApproved { get; init; }
    public int TotalRejected { get; init; }
    public int TotalAllTime { get; init; }
    public double ApprovalRate =>
        TotalAllTime == 0 ? 0
        : Math.Round((double)TotalApproved / TotalAllTime * 100, 1);
}