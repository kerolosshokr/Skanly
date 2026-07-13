// Skanly.Application/Features/Reports/Interfaces/IReportService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Reports.DTOs;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Features.Reports.Interfaces;

public interface IReportService
{
    // ── Any authenticated user ─────────────────────────────────────────────────

    /// <summary>
    /// Submits a new report. Enforces duplicate guard —
    /// cannot report the same target for the same type while
    /// an open/investigating report already exists.
    /// </summary>
    Task<ServiceResult<ReportDto>> CreateAsync(
        string reporterId,
        CreateReportDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all reports submitted by the calling user, paged.
    /// </summary>
    Task<ServiceResult<PagedResult<ReportDto>>> GetByReporterAsync(
        string reporterId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single report — caller must be reporter or Admin.
    /// </summary>
    Task<ServiceResult<ReportDto>> GetByIdAsync(
        string requesterId,
        int reportId,
        bool isAdmin = false,
        CancellationToken ct = default);

    // ── Admin operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all reports with filtering and paging for Admin dashboard.
    /// </summary>
    Task<ServiceResult<PagedResult<ReportDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        ReportStatus? statusFilter = null,
        ReportType? typeFilter = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Transitions a report to UnderInvestigation, Resolved, or Dismissed.
    /// Optionally deactivates the reported user or removes the reported property.
    /// </summary>
    Task<ServiceResult<ReportDto>> ResolveAsync(
        string adminId,
        ResolveReportDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Returns aggregated stats for the Admin dashboard widget.
    /// </summary>
    Task<ServiceResult<ReportSummaryDto>> GetSummaryAsync(
        CancellationToken ct = default);
}