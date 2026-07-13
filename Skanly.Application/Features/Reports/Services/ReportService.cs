// Skanly.Application/Features/Reports/Services/ReportService.cs
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Reports.DTOs;
using Skanly.Application.Features.Reports.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Features.Reports.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _fileStorage;
    private readonly IIdentityService _identityService; 
    private readonly IValidator<CreateReportDto> _createValidator;
    private readonly IValidator<ResolveReportDto> _resolveValidator;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IUnitOfWork uow,
        IFileStorageService fileStorage,
        IIdentityService identityService,
        IValidator<CreateReportDto> createValidator,
        IValidator<ResolveReportDto> resolveValidator,
        ILogger<ReportService> logger)
    {
        _uow = uow;
        _fileStorage = fileStorage;
        _identityService = identityService;
        _createValidator = createValidator;
        _resolveValidator = resolveValidator;
        _logger = logger;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<ReportDto>> CreateAsync(
        string reporterId,
        CreateReportDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate input
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<ReportDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Cannot report yourself
        if (dto.ReportedUserId == reporterId)
            return ServiceResult<ReportDto>.Failure(
                "You cannot submit a report against yourself.");

        // 3. Duplicate guard — prevent spam reporting
        var duplicateExists = await _uow.Repository<Report>()
            .ExistsAsync(r =>
                r.ReporterId == reporterId &&
                r.ReportType == dto.ReportType &&
                r.ReportedPropertyId == dto.ReportedPropertyId &&
                r.ReportedUserId == dto.ReportedUserId &&
                (r.Status == ReportStatus.Open ||
                 r.Status == ReportStatus.UnderInvestigation), ct);

        if (duplicateExists)
            return ServiceResult<ReportDto>.Failure(
                "You have already submitted a report of this type for this target. " +
                "Our team is reviewing it. " +
                "Please wait for resolution before submitting another.");

        // 4. Upload evidence if provided
        string? evidenceUrl = null;
        if (dto.Evidence is not null)
        {
            evidenceUrl = await _fileStorage.SaveAsync(
                dto.Evidence, $"reports/{reporterId}", ct);
        }

        // 5. Create report
        var report = new Report
        {
            ReporterId = reporterId,
            ReportedPropertyId = dto.ReportedPropertyId,
            ReportedUserId = dto.ReportedUserId,
            ReportType = dto.ReportType,
            Description = dto.Description.Trim(),
            EvidenceUrl = evidenceUrl,
            Status = ReportStatus.Open
        };

        await _uow.Repository<Report>().AddAsync(report, ct);

        // 6. Notify all admins
        var admins = await _uow.Repository<Admin>().GetAllAsync(ct);
        foreach (var admin in admins)
        {
            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = admin.UserId,
                Title = "New Report Submitted",
                Message = $"A new {report.ReportType} report requires review.",
                Type = NotificationType.BookingUpdate,
                RelatedEntityId = report.Id,
                RelatedEntityType = "Report"
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Report {ReportId} submitted by {ReporterId} — Type: {Type}",
            report.Id, reporterId, dto.ReportType);

        return await BuildReportDtoResultAsync(report.Id, ct);
    }

    // ── GetByReporterAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<ReportDto>>> GetByReporterAsync(
        string reporterId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var reports = await _uow.Reports
            .GetByReporterIdAsync(reporterId, ct);

        var total = reports.Count;
        var paged = reports
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<ReportDto>();
        foreach (var r in paged)
            dtos.Add(await MapToDto(r, ct));

        return ServiceResult<PagedResult<ReportDto>>.Success(
            PagedResult<ReportDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<ReportDto>> GetByIdAsync(
        string requesterId,
        int reportId,
        bool isAdmin = false,
        CancellationToken ct = default)
    {
        var report = await _uow.Reports.GetDetailAsync(reportId, ct);

        if (report is null)
            return ServiceResult<ReportDto>.Failure("Report not found.");

        // Non-admins can only see their own reports
        if (!isAdmin && report.ReporterId != requesterId)
            return ServiceResult<ReportDto>.Failure("Access denied.");

        var dto = await MapToDto(report, ct);
        return ServiceResult<ReportDto>.Success(dto);
    }

    // ── GetAllAsync (Admin) ───────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<ReportDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        ReportStatus? statusFilter = null,
        ReportType? typeFilter = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Repository<Report>()
            .GetPagedAsync(
                pageNumber,
                pageSize,
                predicate: r =>
                    (statusFilter == null || r.Status == statusFilter) &&
                    (typeFilter == null || r.ReportType == typeFilter) &&
                    (searchTerm == null ||
                     r.Description.Contains(searchTerm)),
                orderBy: q => q
                    .OrderBy(r => r.Status)
                    .ThenByDescending(r => r.CreatedAt),
                ct: ct);

        var dtos = new List<ReportDto>();
        foreach (var r in items)
            dtos.Add(await MapToDto(r, ct));

        return ServiceResult<PagedResult<ReportDto>>.Success(
            PagedResult<ReportDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── ResolveAsync (Admin) ──────────────────────────────────────────────────

    public async Task<ServiceResult<ReportDto>> ResolveAsync(
        string adminId,
        ResolveReportDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate
        var validation = await _resolveValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<ReportDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Load report
        var report = await _uow.Reports.GetDetailAsync(dto.ReportId, ct);
        if (report is null)
            return ServiceResult<ReportDto>.Failure("Report not found.");

        // 3. Guard: can only progress the workflow, not regress
        if (report.Status == ReportStatus.Resolved ||
            report.Status == ReportStatus.Dismissed)
            return ServiceResult<ReportDto>.Failure(
                $"This report is already {report.Status} and cannot be updated further.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // 4. Update report status
            report.Status = dto.NewStatus;
            report.ResolvedByAdminId = adminId;
            report.Resolution = dto.Resolution.Trim();
            report.ResolvedAt = dto.NewStatus == ReportStatus.Resolved ||
                                        dto.NewStatus == ReportStatus.Dismissed
                                            ? DateTime.UtcNow
                                            : null;

            _uow.Repository<Report>().Update(report);

            // 5. Optional: Deactivate reported user
            if (dto.DeactivateUser &&
                !string.IsNullOrEmpty(report.ReportedUserId) &&
                (dto.NewStatus == ReportStatus.Resolved))
            {
                var success = await _identityService.DeactivateUserAsync(
                 report.ReportedUserId!,
                    ct);

                if (success)
                {
                    _logger.LogWarning(
                        "User {UserId} deactivated following report {ReportId} resolution",
                        report.ReportedUserId,
                        report.Id);
                }
            }

            // 6. Optional: Soft-delete reported property
            if (dto.RemoveProperty &&
                report.ReportedPropertyId.HasValue &&
                dto.NewStatus == ReportStatus.Resolved)
            {
                var property = await _uow.Repository<Property>()
                    .GetByIdAsync(report.ReportedPropertyId.Value, ct);

                if (property is not null)
                {
                    property.IsDeleted = true;
                    property.IsAvailable = false;
                    _uow.Repository<Property>().Update(property);

                    _logger.LogWarning(
                        "Property {PropertyId} soft-deleted following report {ReportId}",
                        report.ReportedPropertyId, report.Id);
                }
            }

            // 7. Notify the reporter about the outcome
            if (dto.NewStatus == ReportStatus.Resolved ||
                dto.NewStatus == ReportStatus.Dismissed)
            {
                var notificationMessage = dto.NewStatus == ReportStatus.Resolved
                    ? "Your report has been reviewed and resolved. " +
                      $"Resolution: {dto.Resolution}"
                    : "Your report has been reviewed and dismissed. " +
                      $"Notes: {dto.Resolution}";

                await _uow.Notifications.AddAsync(new Notification
                {
                    UserId = report.ReporterId,
                    Title = $"Report {dto.NewStatus}",
                    Message = notificationMessage,
                    Type = NotificationType.BookingUpdate,
                    RelatedEntityId = report.Id,
                    RelatedEntityType = "Report"
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Report {ReportId} updated to {Status} by Admin {AdminId}",
                dto.ReportId, dto.NewStatus, adminId);

            return await BuildReportDtoResultAsync(report.Id, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── GetSummaryAsync (Admin) ───────────────────────────────────────────────

    public async Task<ServiceResult<ReportSummaryDto>> GetSummaryAsync(
        CancellationToken ct = default)
    {
        var all = await _uow.Repository<Report>().GetAllAsync(ct);

        var byType = all
            .GroupBy(r => r.ReportType)
            .Select(g => new TypeBreakdown
            {
                TypeDisplay = g.Key switch
                {
                    ReportType.FakeListing => "Fake Listing",
                    ReportType.FraudulentOwner => "Fraudulent Owner",
                    ReportType.InappropriateContent => "Inappropriate Content",
                    ReportType.PropertyIssue => "Property Issue",
                    _ => g.Key.ToString()
                },
                Icon = g.Key switch
                {
                    ReportType.FakeListing => "fa-home",
                    ReportType.FraudulentOwner => "fa-user-slash",
                    ReportType.InappropriateContent => "fa-ban",
                    ReportType.PropertyIssue => "fa-wrench",
                    _ => "fa-flag"
                },
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var summary = new ReportSummaryDto
        {
            TotalOpen = all.Count(r => r.Status == ReportStatus.Open),
            TotalUnderInvestigation = all.Count(r => r.Status == ReportStatus.UnderInvestigation),
            TotalResolved = all.Count(r => r.Status == ReportStatus.Resolved),
            TotalDismissed = all.Count(r => r.Status == ReportStatus.Dismissed),
            TotalAllTime = all.Count,
            ByType = byType
        };

        return ServiceResult<ReportSummaryDto>.Success(summary);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<ServiceResult<ReportDto>> BuildReportDtoResultAsync(
        int reportId,
        CancellationToken ct)
    {
        var report = await _uow.Reports.GetDetailAsync(reportId, ct);
        if (report is null)
            return ServiceResult<ReportDto>.Failure("Report not found after save.");

        var dto = await MapToDto(report, ct);
        return ServiceResult<ReportDto>.Success(dto);
    }

    private async Task<ReportDto> MapToDto(Report r, CancellationToken ct)
    {
        // Resolve reporter display info
        var (reporterName, reporterImg) =
     await TryResolveUserAsync(r.ReporterId, ct);

        // Resolve reported user display info
        string? reportedUserName = null;
        string? reportedUserImg = null;

        if (!string.IsNullOrEmpty(r.ReportedUserId))
        {
            (reportedUserName, reportedUserImg) =
                await TryResolveUserAsync(r.ReportedUserId, ct);
        }

        // Resolve admin display name
        string? resolvedByAdminName = null;
        if (!string.IsNullOrEmpty(r.ResolvedByAdminId))
        {
             // use string key lookup below
            var adminEntity = await _uow.Repository<Admin>()
                .GetFirstOrDefaultAsync(
                    a => a.UserId == r.ResolvedByAdminId, ct);
            resolvedByAdminName = adminEntity?.FullName;
        }

        // Property info
        string? propTitle = r.ReportedProperty?.Title;
        string? propImg = r.ReportedProperty?.Images
                               .FirstOrDefault(i => i.IsPrimary)?.ImageUrl;

        if (r.ReportedPropertyId.HasValue && propTitle is null)
        {
            var prop = await _uow.Properties
                .GetDetailAsync(r.ReportedPropertyId.Value, ct);
            propTitle = prop?.Title;
            propImg = prop?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl;
        }

        return new ReportDto
        {
            ReportId = r.Id,
            ReporterId = r.ReporterId,
            ReporterFullName = reporterName,
            ReporterImageUrl = reporterImg,
            ReportedPropertyId = r.ReportedPropertyId,
            ReportedPropertyTitle = propTitle,
            ReportedPropertyImageUrl = propImg,
            ReportedUserId = r.ReportedUserId,
            ReportedUserFullName = reportedUserName,
            ReportedUserImageUrl = reportedUserImg,
            ReportType = r.ReportType,
            Description = r.Description,
            EvidenceUrl = r.EvidenceUrl,
            Status = r.Status,
            ResolvedByAdminId = r.ResolvedByAdminId,
            ResolvedByAdminName = resolvedByAdminName,
            Resolution = r.Resolution,
            ResolvedAt = r.ResolvedAt,
            CreatedAt = r.CreatedAt
        };
    }

    private async Task<(string FullName, string? ImageUrl)> TryResolveUserAsync(
    string userId,
    CancellationToken ct)
    {
        var student = await _uow.Students.GetByUserIdAsync(userId, ct);

        if (student is not null)
            return (student.FullName, student.ProfileImageUrl);

        var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);

        if (owner is not null)
            return (owner.FullName, owner.ProfileImageUrl);

        return ("Unknown User", null);
    }
}