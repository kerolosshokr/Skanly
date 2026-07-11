// Skanly.Application/Features/Students/Services/StudentService.cs
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;          //  فقط Application interfaces
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Bookings.DTOs;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Application.Features.Students.DTOs;
using Skanly.Application.Features.Students.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

// لا يوجد أي using يشير إلى Skanly.Infrastructure

namespace Skanly.Application.Features.Students.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _fileStorage;
    private readonly IIdentityService _identityService;   //  بدل UserManager
    private readonly IValidator<UpdateProfileDto> _updateValidator;
    private readonly IValidator<CompleteProfileDto> _completeValidator;
    private readonly IValidator<UploadIdentityDto> _identityValidator;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        IUnitOfWork uow,
        IFileStorageService fileStorage,
        IIdentityService identityService,              // ✅
        IValidator<UpdateProfileDto> updateValidator,
        IValidator<CompleteProfileDto> completeValidator,
        IValidator<UploadIdentityDto> identityValidator,
        ILogger<StudentService> logger)
    {
        _uow = uow;
        _fileStorage = fileStorage;
        _identityService = identityService;         // ✅
        _updateValidator = updateValidator;
        _completeValidator = completeValidator;
        _identityValidator = identityValidator;
        _logger = logger;
    }

    // ── GetProfileAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult<StudentProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken ct = default)
    {
        var student = await _uow.Students.GetWithUniversityAsync(userId, ct);
        if (student is null)
            return ServiceResult<StudentProfileDto>.Failure(
                "Student profile not found.");

        // ✅ استخدام IIdentityService بدل UserManager
        var email = await _identityService.GetEmailAsync(userId, ct);
        if (email is null)
            return ServiceResult<StudentProfileDto>.Failure(
                "User account not found.");

        var phone = await _identityService.GetPhoneNumberAsync(userId, ct);

        var verification = await _uow.Repository<IdentityVerification>()
            .GetFirstOrDefaultAsync(v => v.UserId == userId, ct);

        var verificationStatus = verification?.Status switch
        {
            VerificationStatus.Pending => "Pending Review",
            VerificationStatus.Approved => "Verified",
            VerificationStatus.Rejected => "Rejected",
            _ => student.IsIdentityVerified ? "Verified" : "Not Submitted"
        };

        var totalBookings = await _uow.Bookings.CountAsync(
            b => b.StudentId == userId, ct);
        var activeBookings = await _uow.Bookings.CountAsync(
            b => b.StudentId == userId &&
                 (b.Status == BookingStatus.Confirmed ||
                  b.Status == BookingStatus.Accepted), ct);
        var totalFavorites = await _uow.Favorites.CountAsync(
            f => f.StudentId == userId, ct);
        var totalReviews = await _uow.Reviews.CountAsync(
            r => r.StudentId == userId, ct);

        var dto = new StudentProfileDto
        {
            UserId = student.UserId,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = email,
            PhoneNumber = phone,
            GenderDisplay = student.Gender == Gender.Male ? "Male" : "Female",
            BirthDate = student.BirthDate,
            NationalId = student.NationalId,
            ProfileImageUrl = student.ProfileImageUrl,
            IsIdentityVerified = student.IsIdentityVerified,
            VerificationStatus = verificationStatus,
            UniversityId = student.UniversityId,
            UniversityNameEn = student.University?.NameEn,
            UniversityNameAr = student.University?.NameAr,
            IsProfileComplete = IsProfileComplete(student, phone),
            CreatedAt = student.CreatedAt,
            TotalBookings = totalBookings,
            ActiveBookings = activeBookings,
            TotalFavorites = totalFavorites,
            TotalReviews = totalReviews
        };

        return ServiceResult<StudentProfileDto>.Success(dto);
    }

    // ── UpdateProfileAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<StudentProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateProfileDto dto,
        CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<StudentProfileDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        var student = await _uow.Students.GetByUserIdAsync(userId, ct);
        if (student is null)
            return ServiceResult<StudentProfileDto>.Failure("Student not found.");

        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.BirthDate = dto.BirthDate;
        student.UniversityId = dto.UniversityId;
        _uow.Students.Update(student);

        // ✅ IIdentityService بدل _userManager.UpdateAsync
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            await _identityService.UpdatePhoneNumberAsync(
                userId, dto.PhoneNumber, ct);
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Student profile updated: {UserId}", userId);

        return await GetProfileAsync(userId, ct);
    }

    // ── CompleteProfileAsync ──────────────────────────────────────────────────

    public async Task<ServiceResult> CompleteProfileAsync(
        string userId,
        CompleteProfileDto dto,
        CancellationToken ct = default)
    {
        var validation = await _completeValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        var student = await _uow.Students.GetByUserIdAsync(userId, ct);
        if (student is null)
            return ServiceResult.Failure("Student not found.");

        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Gender = (Gender)dto.Gender;
        student.BirthDate = dto.BirthDate;
        student.UniversityId = dto.UniversityId;
        _uow.Students.Update(student);

        // ✅
        await _identityService.UpdatePhoneNumberAsync(
            userId, dto.PhoneNumber, ct);

        await _uow.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    // ── UploadProfileImageAsync ───────────────────────────────────────────────

    public async Task<ServiceResult<string>> UploadProfileImageAsync(
        string userId,
        IFormFile image,
        CancellationToken ct = default)
    {
        if (!_fileStorage.IsImageFile(image))
            return ServiceResult<string>.Failure(
                "Only image files (JPG, PNG, WEBP) are accepted.");

        if (image.Length > 3 * 1024 * 1024)
            return ServiceResult<string>.Failure(
                "Image size must not exceed 3 MB.");

        var student = await _uow.Students.GetByUserIdAsync(userId, ct);
        if (student is null)
            return ServiceResult<string>.Failure("Student not found.");

        if (!string.IsNullOrEmpty(student.ProfileImageUrl))
            await _fileStorage.DeleteAsync(student.ProfileImageUrl, ct);

        var imageUrl = await _fileStorage.SaveAsync(
            image, $"profiles/students/{userId}", ct);

        student.ProfileImageUrl = imageUrl;
        _uow.Students.Update(student);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult<string>.Success(imageUrl);
    }
    // ── SubmitIdentityVerificationAsync ───────────────────────────────────────
    public async Task<ServiceResult> SubmitIdentityVerificationAsync(
        string userId,
        UploadIdentityDto dto,
        CancellationToken ct = default)
    {
        var validation = await _identityValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // Only allow one pending submission at a time
        var pendingExists = await _uow.Repository<IdentityVerification>()
            .ExistsAsync(v => v.UserId == userId &&
                              v.Status == VerificationStatus.Pending, ct);

        if (pendingExists)
            return ServiceResult.Failure(
                "You already have a pending verification request. " +
                "Please wait for Admin review before submitting again.");

        var frontUrl = await _fileStorage.SaveAsync(
            dto.NationalIdFront, $"identity/{userId}", ct);

        string? backUrl = null;
        if (dto.NationalIdBack is not null)
            backUrl = await _fileStorage.SaveAsync(
                dto.NationalIdBack, $"identity/{userId}", ct);

        var verification = new IdentityVerification
        {
            UserId = userId,
            NationalIdFrontUrl = frontUrl,
            NationalIdBackUrl = backUrl,
            Status = VerificationStatus.Pending
        };

        await _uow.Repository<IdentityVerification>().AddAsync(verification, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Identity verification submitted for student: {UserId}", userId);

        return ServiceResult.Success();
    }
    // ── GetVerificationStatusAsync ────────────────────────────────────────────
    public async Task<ServiceResult<string>> GetVerificationStatusAsync(
       string userId,
       CancellationToken ct = default)
    {
        var student = await _uow.Students.GetByUserIdAsync(userId, ct);
        if (student is null)
            return ServiceResult<string>.Failure("Student not found.");

        if (student.IsIdentityVerified)
            return ServiceResult<string>.Success("Verified");

        var latest = await _uow.Repository<IdentityVerification>()
            .GetFirstOrDefaultAsync(v => v.UserId == userId, ct);

        var status = latest?.Status switch
        {
            VerificationStatus.Pending => "Pending Review",
            VerificationStatus.Rejected => "Rejected",
            _ => "Not Submitted"
        };

        return ServiceResult<string>.Success(status);
    }
    // ── GetDashboardAsync ─────────────────────────────────────────────────────
    public async Task<ServiceResult<StudentDashboardDto>> GetDashboardAsync(
       string userId,
       CancellationToken ct = default)
    {
        var profileResult = await GetProfileAsync(userId, ct);
        if (!profileResult.IsSuccess)
            return ServiceResult<StudentDashboardDto>.Failure(
                profileResult.ErrorMessage!);

        var profile = profileResult.Data!;

        // Pending bookings
        var pendingBookings = await _uow.Bookings.CountAsync(
            b => b.StudentId == userId && b.Status == BookingStatus.Pending, ct);

        // Unread notifications
        var unreadCount = await _uow.Notifications.GetUnreadCountAsync(userId, ct);

        // Recent bookings (last 5)
        var (recentBookingEntities, _) = await _uow.Bookings.GetByStudentIdAsync(
            userId, 1, 5, null, ct);

        var recentBookings = recentBookingEntities
            .Select(b => new BookingSummaryDto
            {
                BookingId = b.Id,
                PropertyTitle = b.Property.Title,
                PropertyImageUrl = b.Property.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                AreaNameEn = b.Property.Area.NameEn,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                CheckInDate = b.CheckInDate,
                RequestedAt = b.RequestedAt
            })
            .ToList();

        // Recent favorites (last 6)
        var (recentFavoriteEntities, _) = await _uow.Favorites.GetByStudentIdAsync(
            userId, ct) is var favResult
            ? (favResult.Take(6).ToList(), 0)
            : (new List<Favorite>(), 0);

        var recentFavorites = (await _uow.Favorites.GetByStudentIdAsync(userId, ct))
            .Take(6)
            .Select(f => new PropertyCardDto
            {
                PropertyId = f.Property.Id,
                Title = f.Property.Title,
                AreaNameEn = f.Property.Area.NameEn,
                UniversityNameEn = f.Property.University?.NameEn,
                PricePerMonth = f.Property.PricePerMonth,
                PropertyTypeDisplay = f.Property.PropertyType.ToString(),
                AverageRating = f.Property.AverageRating,
                PrimaryImageUrl = f.Property.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                IsFavorited = true
            })
            .ToList();

        // Recent notifications (last 5)
        var (notifEntities, _) = await _uow.Notifications.GetByUserIdAsync(
            userId, 1, 5, ct);

        var recentNotifications = notifEntities
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                CreatedAt = n.CreatedAt
            })
            .ToList();

        var dashboard = new StudentDashboardDto
        {
            FullName = profile.FullName,
            ProfileImageUrl = profile.ProfileImageUrl,
            IsIdentityVerified = profile.IsIdentityVerified,
            IsProfileComplete = profile.IsProfileComplete,
            UniversityNameEn = profile.UniversityNameEn,
            VerificationStatus = profile.VerificationStatus,
            TotalBookings = profile.TotalBookings,
            ActiveBookings = profile.ActiveBookings,
            PendingBookings = pendingBookings,
            TotalFavorites = profile.TotalFavorites,
            UnreadNotifications = unreadCount,
            RecentBookings = recentBookings,
            RecentFavorites = recentFavorites,
            RecentNotifications = recentNotifications
        };

        return ServiceResult<StudentDashboardDto>.Success(dashboard);
    }

    // ── GetBookingsAsync ──────────────────────────────────────────────────────
    public async Task<ServiceResult<PagedResult<BookingSummaryDto>>> GetBookingsAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 10,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        BookingStatus? status = null;
        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<BookingStatus>(statusFilter, out var parsed))
            status = parsed;

        var (items, total) = await _uow.Bookings
            .GetByStudentIdAsync(userId, pageNumber, pageSize, status, ct);

        var dtos = items.Select(b => new BookingSummaryDto
        {
            BookingId = b.Id,
            PropertyTitle = b.Property.Title,
            PropertyImageUrl = b.Property.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            AreaNameEn = b.Property.Area.NameEn,
            TotalAmount = b.TotalAmount,
            Status = b.Status,
            CheckInDate = b.CheckInDate,
            RequestedAt = b.RequestedAt
        }).ToList();

        return ServiceResult<PagedResult<BookingSummaryDto>>.Success(
            PagedResult<BookingSummaryDto>.Create(dtos, total, pageNumber, pageSize));
    }
    // ── GetFavoritesAsync ─────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<PropertyCardDto>>> GetFavoritesAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        var allFavorites = await _uow.Favorites.GetByStudentIdAsync(userId, ct);

        var total = allFavorites.Count;
        var paged = allFavorites
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new PropertyCardDto
            {
                PropertyId = f.Property.Id,
                Title = f.Property.Title,
                AreaNameEn = f.Property.Area.NameEn,
                UniversityNameEn = f.Property.University?.NameEn,
                PricePerMonth = f.Property.PricePerMonth,
                PropertyTypeDisplay = f.Property.PropertyType.ToString(),
                AverageRating = f.Property.AverageRating,
                PrimaryImageUrl = f.Property.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                IsFavorited = true
            })
            .ToList();

        return ServiceResult<PagedResult<PropertyCardDto>>.Success(
            PagedResult<PropertyCardDto>.Create(paged, total, pageNumber, pageSize));
    }
    // ── GetNotificationsAsync ─────────────────────────────────────────────────
    public async Task<ServiceResult<PagedResult<NotificationDto>>> GetNotificationsAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Notifications
            .GetByUserIdAsync(userId, pageNumber, pageSize, ct);

        var dtos = items.Select(n => new NotificationDto
        {
            NotificationId = n.NotificationId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            RelatedEntityId = n.RelatedEntityId,
            RelatedEntityType = n.RelatedEntityType,
            CreatedAt = n.CreatedAt
        }).ToList();

        return ServiceResult<PagedResult<NotificationDto>>.Success(
            PagedResult<NotificationDto>.Create(dtos, total, pageNumber, pageSize));
    }
    // ── MarkNotificationReadAsync ─────────────────────────────────────────────
    public async Task<ServiceResult> MarkNotificationReadAsync(
        string userId,
        long notificationId,
        CancellationToken ct = default)
    {
        var notification = await _uow.Repository<Notification>()
            .GetFirstOrDefaultAsync(
                n => n.NotificationId == notificationId && n.UserId == userId, ct);

        if (notification is null)
            return ServiceResult.Failure("Notification not found.");

        await _uow.Notifications.MarkAsReadAsync(notificationId, ct);
        return ServiceResult.Success();
    }
    // ── MarkAllNotificationsReadAsync ─────────────────────────────────────────
    public async Task<ServiceResult> MarkAllNotificationsReadAsync(
       string userId,
       CancellationToken ct = default)
    {
        await _uow.Notifications.MarkAllAsReadAsync(userId, ct);
        return ServiceResult.Success();
    }
    public async Task<ServiceResult<int>> GetUnreadNotificationCountAsync(
        string userId,
        CancellationToken ct = default)
    {
        var count = await _uow.Notifications.GetUnreadCountAsync(userId, ct);
        return ServiceResult<int>.Success(count);
    }
    // ── GetUnreadNotificationCountAsync ──────────────────────────────────────

    private static bool IsProfileComplete(Student student, string? phone) =>
        !string.IsNullOrEmpty(student.FirstName) &&
        !string.IsNullOrEmpty(student.LastName) &&
        !string.IsNullOrEmpty(phone) &&
        student.Gender != 0;
}