// Skanly.Application/Features/Verification/Services/VerificationService.cs
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Application.Features.Verification.DTOs;
using Skanly.Application.Features.Verification.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Verification.Services;

public class VerificationService : IVerificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IOcrService _ocrService;
    private readonly IFileStorageService _fileStorage;
    private readonly IIdentityService _identityService;      // ✅ not UserManager
    private readonly INotificationService _notificationService;
    private readonly IValidator<SubmitVerificationDto> _submitValidator;
    private readonly IValidator<ReviewVerificationDto> _reviewValidator;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IUnitOfWork uow,
        IOcrService ocrService,
        IFileStorageService fileStorage,
        IIdentityService identityService,
        INotificationService notificationService,
        IValidator<SubmitVerificationDto> submitValidator,
        IValidator<ReviewVerificationDto> reviewValidator,
        ILogger<VerificationService> logger)
    {
        _uow = uow;
        _ocrService = ocrService;
        _fileStorage = fileStorage;
        _identityService = identityService;
        _notificationService = notificationService;
        _submitValidator = submitValidator;
        _reviewValidator = reviewValidator;
        _logger = logger;
    }

    // ── SubmitAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<VerificationDto>> SubmitAsync(
        string userId,
        SubmitVerificationDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate files
        var validation = await _submitValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<VerificationDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Block if already verified
        var isVerified = await IsUserVerifiedAsync(userId, ct);
        if (isVerified)
            return ServiceResult<VerificationDto>.Failure(
                "Your identity is already verified.");

        // 3. Block if pending submission exists
        var pendingExists = await _uow.Repository<IdentityVerification>()
            .ExistsAsync(v => v.UserId == userId &&
                              v.Status == VerificationStatus.Pending, ct);

        if (pendingExists)
            return ServiceResult<VerificationDto>.Failure(
                "You already have a pending verification request. " +
                "Please wait for Admin review before submitting again.");

        // 4. Save front image
        var frontUrl = await _fileStorage.SaveAsync(
            dto.NationalIdFront,
            $"identity/{userId}/front",
            ct);

        // 5. Save back image (optional)
        string? backUrl = null;
        if (dto.NationalIdBack is not null)
            backUrl = await _fileStorage.SaveAsync(
                dto.NationalIdBack,
                $"identity/{userId}/back",
                ct);

        // 6. Run OCR on front image
        OcrResultDto? ocrResult = null;
        string? extractedName = null;
        string? extractedNationalId = null;
        DateOnly? extractedBirthDate = null;
        double? ocrConfidence = null;

        try
        {
            await using var stream = dto.NationalIdFront.OpenReadStream();
            ocrResult = await _ocrService.ExtractFromImageAsync(
                stream, dto.NationalIdFront.FileName, ct);

            if (ocrResult.IsSuccess)
            {
                extractedName = ocrResult.ExtractedName;
                extractedNationalId = ocrResult.ExtractedNationalId;
                extractedBirthDate = ocrResult.ExtractedBirthDate;
                ocrConfidence = ocrResult.ConfidenceScore;

                // 7. Validate Egyptian ID format if extracted
                if (!string.IsNullOrEmpty(extractedNationalId))
                {
                    var idValidation = _ocrService
                        .ValidateEgyptianId(extractedNationalId);

                    if (!idValidation.IsValid)
                    {
                        _logger.LogWarning(
                            "OCR extracted invalid ID format for user {UserId}: {Reason}",
                            userId, idValidation.Reason);
                        // Don't fail submission — Admin will review manually
                        extractedNationalId = null;
                    }
                    else if (!extractedBirthDate.HasValue)
                    {
                        extractedBirthDate = idValidation.EncodedBirthDate;
                    }
                }

                _logger.LogInformation(
                    "OCR completed for user {UserId}. " +
                    "Confidence={Confidence:F1}% Name={Name} ID={Id}",
                    userId, ocrConfidence,
                    extractedName ?? "not found",
                    extractedNationalId ?? "not found");
            }
            else
            {
                _logger.LogWarning(
                    "OCR failed for user {UserId}: {Reason}",
                    userId, ocrResult.FailureReason);
            }
        }
        catch (Exception ex)
        {
            // OCR failure should never block submission
            _logger.LogError(ex,
                "OCR exception for user {UserId} — " +
                "proceeding with manual review", userId);
        }

        // 8. Create verification record
        var verification = new IdentityVerification
        {
            UserId = userId,
            NationalIdFrontUrl = frontUrl,
            NationalIdBackUrl = backUrl,
            ExtractedName = extractedName,
            ExtractedNationalId = extractedNationalId,
            ExtractedBirthDate = extractedBirthDate,
            Status = VerificationStatus.Pending
        };

        await _uow.Repository<IdentityVerification>().AddAsync(verification, ct);
        await _uow.SaveChangesAsync(ct);

        // 9. Notify Admins
        var admins = await _uow.Repository<Admin>().GetAllAsync(ct);
        foreach (var admin in admins)
        {
            await _notificationService.SendAsync(
                userId: admin.UserId,
                title: "New Identity Verification Request",
                message: $"A user has submitted identity documents " +
                                    $"for review. " +
                                    $"OCR Confidence: {ocrConfidence:F0}%",
                type: NotificationType.VerificationApproval,
                relatedEntityId: verification.Id,
                relatedEntityType: "Verification",
                ct: ct);
        }

        _logger.LogInformation(
            "Verification {VerificationId} submitted by {UserId}",
            verification.Id, userId);

        var resultDto = await BuildDtoAsync(verification, ct);
        return ServiceResult<VerificationDto>.Success(resultDto);
    }

    // ── GetLatestForUserAsync ─────────────────────────────────────────────────

    public async Task<ServiceResult<VerificationDto?>> GetLatestForUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        var verification = await _uow.Repository<IdentityVerification>()
            .GetFirstOrDefaultAsync(
                v => v.UserId == userId,
                ct,
                v => v.ReviewedByAdmin!);

        if (verification is null)
            return ServiceResult<VerificationDto?>.Success(null);

        var dto = await BuildDtoAsync(verification, ct);
        return ServiceResult<VerificationDto?>.Success(dto);
    }

    // ── GetHistoryAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<VerificationDto>>>
        GetHistoryAsync(
            string userId,
            CancellationToken ct = default)
    {
        var verifications = await _uow.Repository<IdentityVerification>()
            .GetAllAsync(v => v.UserId == userId, ct);

        var dtos = new List<VerificationDto>();
        foreach (var v in verifications.OrderByDescending(x => x.CreatedAt))
            dtos.Add(await BuildDtoAsync(v, ct));

        return ServiceResult<IReadOnlyList<VerificationDto>>.Success(dtos);
    }

    // ── GetPendingAsync (Admin) ───────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<VerificationDto>>>
        GetPendingAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken ct = default)
    {
        var (items, total) = await _uow.Repository<IdentityVerification>()
            .GetPagedAsync(
                pageNumber,
                pageSize,
                predicate: v => v.Status == VerificationStatus.Pending,
                orderBy: q => q.OrderBy(v => v.CreatedAt),
                ct: ct);

        var dtos = new List<VerificationDto>();
        foreach (var v in items)
            dtos.Add(await BuildDtoAsync(v, ct));

        return ServiceResult<PagedResult<VerificationDto>>.Success(
            PagedResult<VerificationDto>.Create(
                dtos, total, pageNumber, pageSize));
    }

    // ── GetAllAsync (Admin) ───────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<VerificationDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        VerificationStatus? statusFilter = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Repository<IdentityVerification>()
            .GetPagedAsync(
                pageNumber,
                pageSize,
                predicate: v =>
                    (statusFilter == null || v.Status == statusFilter) &&
                    (searchTerm == null ||
                     (v.ExtractedName != null &&
                      v.ExtractedName.Contains(searchTerm)) ||
                     (v.ExtractedNationalId != null &&
                      v.ExtractedNationalId.Contains(searchTerm))),
                orderBy: q => q
                    .OrderBy(v => v.Status)
                    .ThenByDescending(v => v.CreatedAt),
                ct: ct);

        var dtos = new List<VerificationDto>();
        foreach (var v in items)
            dtos.Add(await BuildDtoAsync(v, ct));

        return ServiceResult<PagedResult<VerificationDto>>.Success(
            PagedResult<VerificationDto>.Create(
                dtos, total, pageNumber, pageSize));
    }

    // ── GetByIdAsync (Admin) ──────────────────────────────────────────────────

    public async Task<ServiceResult<VerificationDto>> GetByIdAsync(
        int verificationId,
        CancellationToken ct = default)
    {
        var verification = await _uow.Repository<IdentityVerification>()
            .GetByIdAsync(verificationId, ct);

        if (verification is null)
            return ServiceResult<VerificationDto>.Failure(
                "Verification record not found.");

        var dto = await BuildDtoAsync(verification, ct);
        return ServiceResult<VerificationDto>.Success(dto);
    }

    // ── ReviewAsync (Admin) ───────────────────────────────────────────────────

    public async Task<ServiceResult> ReviewAsync(
        string adminId,
        ReviewVerificationDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate
        var validation = await _reviewValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Load verification
        var verification = await _uow.Repository<IdentityVerification>()
            .GetByIdAsync(dto.VerificationId, ct);

        if (verification is null)
            return ServiceResult.Failure("Verification record not found.");

        if (verification.Status != VerificationStatus.Pending)
            return ServiceResult.Failure(
                $"This verification is already {verification.Status} " +
                "and cannot be reviewed again.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // 3. Update verification record
            verification.Status = dto.Decision;
            verification.ReviewedByAdminId = adminId;
            verification.ReviewedAt = DateTime.UtcNow;
            verification.RejectionReason = dto.RejectionReason;

            // Update extracted fields with Admin-corrected values
            if (!string.IsNullOrEmpty(dto.VerifiedName))
                verification.ExtractedName = dto.VerifiedName;
            if (!string.IsNullOrEmpty(dto.VerifiedNationalId))
                verification.ExtractedNationalId = dto.VerifiedNationalId;
            if (dto.VerifiedBirthDate.HasValue)
                verification.ExtractedBirthDate = dto.VerifiedBirthDate;

            _uow.Repository<IdentityVerification>().Update(verification);

            if (dto.Decision == VerificationStatus.Approved)
            {
                // 4a. Update Student or Owner profile
                await ApproveUserProfileAsync(
                    verification.UserId,
                    dto.VerifiedName,
                    dto.VerifiedNationalId,
                    dto.VerifiedBirthDate,
                    ct);

                // 4b. Send approval notification
                await _notificationService.SendVerificationApprovedAsync(
                    verification.UserId, ct);
            }
            else
            {
                // 5. Send rejection notification
                await _notificationService.SendVerificationRejectedAsync(
                    verification.UserId,
                    dto.RejectionReason ?? "Documents could not be verified.",
                    ct);
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Verification {VerificationId} {Decision} by Admin {AdminId}",
                dto.VerificationId, dto.Decision, adminId);

            return ServiceResult.Success();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── GetSummaryAsync (Admin) ───────────────────────────────────────────────

    public async Task<ServiceResult<VerificationSummaryDto>> GetSummaryAsync(
        CancellationToken ct = default)
    {
        var all = await _uow.Repository<IdentityVerification>()
            .GetAllAsync(ct);

        var summary = new VerificationSummaryDto
        {
            TotalPending = all.Count(v => v.Status == VerificationStatus.Pending),
            TotalApproved = all.Count(v => v.Status == VerificationStatus.Approved),
            TotalRejected = all.Count(v => v.Status == VerificationStatus.Rejected),
            TotalAllTime = all.Count
        };

        return ServiceResult<VerificationSummaryDto>.Success(summary);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task ApproveUserProfileAsync(
        string userId,
        string? verifiedName,
        string? verifiedNationalId,
        DateOnly? verifiedBirthDate,
        CancellationToken ct)
    {
        // Try Student first
        var student = await _uow.Students.GetByUserIdAsync(userId, ct);
        if (student is not null)
        {
            student.IsIdentityVerified = true;

            if (!string.IsNullOrEmpty(verifiedNationalId))
                student.NationalId = verifiedNationalId;

            if (verifiedBirthDate.HasValue)
                student.BirthDate = verifiedBirthDate;

            // Update name only if student hasn't set their own
            if (!string.IsNullOrEmpty(verifiedName) &&
                string.IsNullOrEmpty(student.FirstName))
            {
                var parts = verifiedName.Split(' ', 2);
                student.FirstName = parts[0];
                student.LastName = parts.Length > 1 ? parts[1] : string.Empty;
            }

            _uow.Students.Update(student);
            return;
        }

        // Try Owner
        var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);
        if (owner is not null)
        {
            owner.IsIdentityVerified = true;

            if (!string.IsNullOrEmpty(verifiedNationalId))
                owner.NationalId = verifiedNationalId;

            _uow.Owners.Update(owner);
        }
    }

    private async Task<bool> IsUserVerifiedAsync(
        string userId,
        CancellationToken ct)
    {
        var student = await _uow.Students.GetByUserIdAsync(userId, ct);
        if (student is not null) return student.IsIdentityVerified;

        var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);
        return owner?.IsIdentityVerified ?? false;
    }

    private async Task<VerificationDto> BuildDtoAsync(
        IdentityVerification v,
        CancellationToken ct)
    {
        // Resolve user display info via IIdentityService ✅
        var email = await _identityService.GetEmailAsync(v.UserId, ct) ?? "";

        // Resolve name from Student or Owner profile
        string userFullName = "Unknown";
        string? userImageUrl = null;
        string userRole = "Unknown";

        var student = await _uow.Students.GetByUserIdAsync(v.UserId, ct);
        if (student is not null)
        {
            userFullName = student.FullName;
            userImageUrl = student.ProfileImageUrl;
            userRole = "Student";
        }
        else
        {
            var owner = await _uow.Owners.GetByUserIdAsync(v.UserId, ct);
            if (owner is not null)
            {
                userFullName = owner.FullName;
                userImageUrl = owner.ProfileImageUrl;
                userRole = "Owner";
            }
        }

        // Admin name
        string? adminName = null;
        if (!string.IsNullOrEmpty(v.ReviewedByAdminId))
        {
            var admin = await _uow.Repository<Admin>()
                .GetFirstOrDefaultAsync(
                    a => a.UserId == v.ReviewedByAdminId, ct);
            adminName = admin?.FullName;
        }

        return new VerificationDto
        {
            VerificationId = v.Id,
            UserId = v.UserId,
            UserFullName = userFullName,
            UserEmail = email,
            UserImageUrl = userImageUrl,
            UserRole = userRole,
            NationalIdFrontUrl = v.NationalIdFrontUrl,
            NationalIdBackUrl = v.NationalIdBackUrl,
            ExtractedName = v.ExtractedName,
            ExtractedNationalId = v.ExtractedNationalId,
            ExtractedBirthDate = v.ExtractedBirthDate,
            OcrConfidenceScore = null,   // not stored in entity — used at submit time
            Status = v.Status,
            ReviewedByAdminName = adminName,
            ReviewedAt = v.ReviewedAt,
            RejectionReason = v.RejectionReason,
            CreatedAt = v.CreatedAt
        };
    }
}