using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Application.Features.Owners.DTOs;
using Skanly.Application.Features.Owners.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Infrastructure.Identity;

namespace Skanly.Application.Features.Owners.Services;

public class OwnerService : IOwnerService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<UpdateOwnerProfileDto> _updateValidator;
    private readonly IValidator<HandleBookingRequestDto> _bookingValidator;
    private readonly ILogger<OwnerService> _logger;

    public OwnerService(
        IUnitOfWork uow,
        IMapper mapper,
        IFileStorageService fileStorage,
        UserManager<ApplicationUser> userManager,
        IValidator<UpdateOwnerProfileDto> updateValidator,
        IValidator<HandleBookingRequestDto> bookingValidator,
        ILogger<OwnerService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _userManager = userManager;
        _updateValidator = updateValidator;
        _bookingValidator = bookingValidator;
        _logger = logger;
    }

    // ── GetProfileAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult<OwnerProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken ct = default)
    {
        var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);
        if (owner is null)
            return ServiceResult<OwnerProfileDto>.Failure("Owner profile not found.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<OwnerProfileDto>.Failure("User account not found.");

        var verification = await _uow.Repository<IdentityVerification>()
            .GetFirstOrDefaultAsync(v => v.UserId == userId, ct);

        var verificationStatus = verification?.Status switch
        {
            VerificationStatus.Pending  => "Pending Review",
            VerificationStatus.Approved => "Verified",
            VerificationStatus.Rejected => "Rejected",
            _                           => owner.IsIdentityVerified
                                            ? "Verified"
                                            : "Not Submitted"
        };

        var totalProperties = await _uow.Properties.CountAsync(
            p => p.OwnerId == userId, ct);
        var activeListings = await _uow.Properties.CountAsync(
            p => p.OwnerId == userId && p.IsAvailable && p.IsApproved, ct);
        var totalBookings = await _uow.Bookings.CountAsync(
            b => b.Property.OwnerId == userId, ct);
        var pendingRequests = await _uow.Bookings.CountAsync(
            b => b.Property.OwnerId == userId &&
                 b.Status == BookingStatus.Pending, ct);
        var totalEarnings = await _uow.Owners.GetTotalEarningsAsync(userId, ct);

        var dto = new OwnerProfileDto
        {
            UserId             = owner.UserId,
            FirstName          = owner.FirstName,
            LastName           = owner.LastName,
            Email              = user.Email!,
            PhoneNumber        = user.PhoneNumber,
            BusinessName       = owner.BusinessName,
            ProfileImageUrl    = owner.ProfileImageUrl,
            IsIdentityVerified = owner.IsIdentityVerified,
            VerificationStatus = verificationStatus,
            CreatedAt          = owner.CreatedAt,
            TotalProperties    = totalProperties,
            ActiveListings     = activeListings,
            TotalBookings      = totalBookings,
            PendingRequests    = pendingRequests,
            TotalEarnings      = totalEarnings
        };

        return ServiceResult<OwnerProfileDto>.Success(dto);
    }

    // ── UpdateProfileAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<OwnerProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateOwnerProfileDto dto,
        CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<OwnerProfileDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);
        if (owner is null)
            return ServiceResult<OwnerProfileDto>.Failure("Owner not found.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<OwnerProfileDto>.Failure("User not found.");

        owner.FirstName       = dto.FirstName;
        owner.LastName        = dto.LastName;
        owner.BusinessName    = dto.BusinessName;
        owner.BankAccountInfo = dto.BankAccountInfo;
        _uow.Owners.Update(owner);

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            user.PhoneNumber = dto.PhoneNumber;
            await _userManager.UpdateAsync(user);
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Owner profile updated: {UserId}", userId);

        return await GetProfileAsync(userId, ct);
    }

    // ── UploadProfileImageAsync ───────────────────────────────────────────────

    public async Task<ServiceResult<string>> UploadProfileImageAsync(
        string userId,
        IFormFile image,
        CancellationToken ct = default)
    {
        if (!_fileStorage.IsImageFile(image))
            return ServiceResult<string>.Failure("Only image files are accepted.");

        if (image.Length > 3 * 1024 * 1024)
            return ServiceResult<string>.Failure("Image must not exceed 3 MB.");

        var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);
        if (owner is null)
            return ServiceResult<string>.Failure("Owner not found.");

        if (!string.IsNullOrEmpty(owner.ProfileImageUrl))
            await _fileStorage.DeleteAsync(owner.ProfileImageUrl, ct);

        var imageUrl = await _fileStorage.SaveAsync(
            image, $"profiles/owners/{userId}", ct);

        owner.ProfileImageUrl = imageUrl;
        _uow.Owners.Update(owner);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult<string>.Success(imageUrl);
    }

    // ── SubmitIdentityVerificationAsync ───────────────────────────────────────

    public async Task<ServiceResult> SubmitIdentityVerificationAsync(
        string userId,
        UploadIdentityDto dto,
        CancellationToken ct = default)
    {
        var pendingExists = await _uow.Repository<IdentityVerification>()
            .ExistsAsync(v => v.UserId == userId &&
                              v.Status == VerificationStatus.Pending, ct);

        if (pendingExists)
            return ServiceResult.Failure(
                "You already have a pending verification request.");

        var frontUrl = await _fileStorage.SaveAsync(
            dto.NationalIdFront, $"identity/{userId}", ct);

        string? backUrl = null;
        if (dto.NationalIdBack is not null)
            backUrl = await _fileStorage.SaveAsync(
                dto.NationalIdBack, $"identity/{userId}", ct);

        var verification = new IdentityVerification
        {
            UserId             = userId,
            NationalIdFrontUrl = frontUrl,
            NationalIdBackUrl  = backUrl,
            Status             = VerificationStatus.Pending
        };

        await _uow.Repository<IdentityVerification>().AddAsync(verification, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Identity verification submitted for owner: {UserId}", userId);

        return ServiceResult.Success();
    }

    // ── GetDashboardAsync ─────────────────────────────────────────────────────

    public async Task<ServiceResult<OwnerDashboardDto>> GetDashboardAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profileResult = await GetProfileAsync(userId, ct);
        if (!profileResult.IsSuccess)
            return ServiceResult<OwnerDashboardDto>.Failure(
                profileResult.ErrorMessage!);

        var profile = profileResult.Data!;

        // Monthly earnings (current month)
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEarnings = await _uow.Bookings
            .GetConfirmedAsync(monthStart, now, ct);

        var monthlyEarnings = monthEarnings
            .Where(b => b.Property.OwnerId == userId)
            .Sum(b => b.TotalAmount - (b.CommissionAmount ?? 0));

        // Unread notifications
        var unreadCount = await _uow.Notifications
            .GetUnreadCountAsync(userId, ct);

        // Pending booking requests (max 5 for dashboard)
        var pendingRequests = await _uow.Bookings
            .GetPendingByOwnerIdAsync(userId, ct);

        var pendingDtos = pendingRequests.Take(5)
            .Select(MapToBookingRequestDto)
            .ToList();

        // Top properties (by booking count)
        var ownerProperties = await _uow.Properties
            .GetByOwnerIdAsync(userId, false, ct);

        var topProperties = new List<OwnerPropertySummaryDto>();
        foreach (var p in ownerProperties.Take(5))
        {
            var bookingCount = await _uow.Bookings.CountAsync(
                b => b.PropertyId == p.Id, ct);
            var activeCount = await _uow.Bookings.CountAsync(
                b => b.PropertyId == p.Id &&
                     b.Status == BookingStatus.Confirmed, ct);

            topProperties.Add(new OwnerPropertySummaryDto
            {
                PropertyId          = p.Id,
                Title               = p.Title,
                AreaNameEn          = p.Area.NameEn,
                PropertyTypeDisplay = p.PropertyType.ToString(),
                PricePerMonth       = p.PricePerMonth,
                AverageRating       = p.AverageRating,
                IsAvailable         = p.IsAvailable,
                IsApproved          = p.IsApproved,
                IsDeleted           = p.IsDeleted,
                PrimaryImageUrl     = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                TotalBookings       = bookingCount,
                ActiveBookings      = activeCount,
                CreatedAt           = p.CreatedAt
            });
        }

        // Recent notifications (last 5)
        var (notifEntities, _) = await _uow.Notifications
            .GetByUserIdAsync(userId, 1, 5, ct);

        var recentNotifications = notifEntities
            .Select(MapToNotificationDto)
            .ToList();

        // Earnings chart — last 6 months
        var earningsChart = await BuildEarningsChartAsync(userId, 6, ct);

        var dashboard = new OwnerDashboardDto
        {
            FullName               = profile.FullName,
            ProfileImageUrl        = profile.ProfileImageUrl,
            IsIdentityVerified     = profile.IsIdentityVerified,
            VerificationStatus     = profile.VerificationStatus,
            TotalProperties        = profile.TotalProperties,
            ActiveListings         = profile.ActiveListings,
            PendingRequests        = profile.PendingRequests,
            TotalBookings          = profile.TotalBookings,
            TotalEarnings          = profile.TotalEarnings,
            MonthlyEarnings        = monthlyEarnings,
            UnreadNotifications    = unreadCount,
            PendingBookingRequests = pendingDtos,
            TopProperties          = topProperties,
            RecentNotifications    = recentNotifications,
            EarningsChart          = earningsChart
        };

        return ServiceResult<OwnerDashboardDto>.Success(dashboard);
    }

    // ── GetPropertiesAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<OwnerPropertySummaryDto>>> GetPropertiesAsync(
        string ownerId,
        int pageNumber = 1,
        int pageSize = 10,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        var allProperties = await _uow.Properties
            .GetByOwnerIdAsync(ownerId, includeDeleted: true, ct);

        // Apply status filter
        var filtered = statusFilter switch
        {
            "Approved"    => allProperties.Where(p => p.IsApproved && !p.IsDeleted),
            "Pending"     => allProperties.Where(p => !p.IsApproved && !p.IsDeleted),
            "Unavailable" => allProperties.Where(p => !p.IsAvailable && !p.IsDeleted),
            "Deleted"     => allProperties.Where(p => p.IsDeleted),
            _             => allProperties.Where(p => !p.IsDeleted)
        };

        var total = filtered.Count();
        var paged = filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<OwnerPropertySummaryDto>();
        foreach (var p in paged)
        {
            var bookingCount = await _uow.Bookings.CountAsync(
                b => b.PropertyId == p.Id, ct);
            var activeCount = await _uow.Bookings.CountAsync(
                b => b.PropertyId == p.Id &&
                     b.Status == BookingStatus.Confirmed, ct);

            dtos.Add(new OwnerPropertySummaryDto
            {
                PropertyId          = p.Id,
                Title               = p.Title,
                AreaNameEn          = p.Area.NameEn,
                PropertyTypeDisplay = p.PropertyType.ToString(),
                PricePerMonth       = p.PricePerMonth,
                AverageRating       = p.AverageRating,
                IsAvailable         = p.IsAvailable,
                IsApproved          = p.IsApproved,
                IsDeleted           = p.IsDeleted,
                PrimaryImageUrl     = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                TotalBookings       = bookingCount,
                ActiveBookings      = activeCount,
                CreatedAt           = p.CreatedAt
            });
        }

        return ServiceResult<PagedResult<OwnerPropertySummaryDto>>.Success(
            PagedResult<OwnerPropertySummaryDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── GetBookingRequestsAsync ───────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<BookingRequestDto>>> GetBookingRequestsAsync(
        string ownerId,
        int pageNumber = 1,
        int pageSize = 10,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        BookingStatus? status = null;
        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<BookingStatus>(statusFilter, out var parsed))
            status = parsed;

        // Get all properties owned
        var ownerPropertyIds = (await _uow.Properties
            .GetByOwnerIdAsync(ownerId, false, ct))
            .Select(p => p.Id)
            .ToHashSet();

        var (allItems, total) = await _uow.Bookings.GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: b => ownerPropertyIds.Contains(b.PropertyId) &&
                            (status == null || b.Status == status),
            orderBy: q => q.OrderByDescending(b => b.RequestedAt),
            ct: ct,
            b => b.Student,
            b => b.Property,
            b => b.Property.Area,
            b => b.Property.Images);

        var dtos = allItems.Select(MapToBookingRequestDto).ToList();

        return ServiceResult<PagedResult<BookingRequestDto>>.Success(
            PagedResult<BookingRequestDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── GetBookingRequestDetailAsync ──────────────────────────────────────────

    public async Task<ServiceResult<BookingRequestDto>> GetBookingRequestDetailAsync(
        string ownerId,
        int bookingId,
        CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult<BookingRequestDto>.Failure("Booking not found.");

        // Ownership guard
        if (booking.Property.OwnerId != ownerId)
            return ServiceResult<BookingRequestDto>.Failure("Access denied.");

        return ServiceResult<BookingRequestDto>.Success(
            MapToBookingRequestDto(booking));
    }

    // ── HandleBookingRequestAsync ─────────────────────────────────────────────

    public async Task<ServiceResult> HandleBookingRequestAsync(
        string ownerId,
        HandleBookingRequestDto dto,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await _bookingValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        var booking = await _uow.Bookings.GetDetailAsync(dto.BookingId, ct);

        if (booking is null)
            return ServiceResult.Failure("Booking not found.");

        if (booking.Property.OwnerId != ownerId)
            return ServiceResult.Failure("Access denied.");

        if (booking.Status != BookingStatus.Pending)
            return ServiceResult.Failure(
                $"Cannot handle a booking that is already {booking.Status}.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // Update booking status
            booking.Status      = dto.Accept ? BookingStatus.Accepted : BookingStatus.Rejected;
            booking.RespondedAt = DateTime.UtcNow;
            _uow.Bookings.Update(booking);

            // Get active commission rate
            var commissionRate = await _uow.Repository<CommissionSetting>()
                .GetFirstOrDefaultAsync(c => c.IsActive, ct);

            if (dto.Accept && commissionRate is not null)
                booking.CommissionRate = commissionRate.Rate;

            // Notify student
            var notification = new Notification
            {
                UserId            = booking.StudentId,
                Title             = dto.Accept ? "Booking Accepted!" : "Booking Rejected",
                Message           = dto.Accept
                    ? $"Your booking request for {booking.Property.Title} has been accepted. Proceed to payment."
                    : $"Your booking request for {booking.Property.Title} was declined. {dto.RejectionReason}",
                Type              = NotificationType.BookingUpdate,
                RelatedEntityId   = booking.Id,
                RelatedEntityType = "Booking"
            };

            await _uow.Notifications.AddAsync(notification, ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Booking {BookingId} {Action} by owner {OwnerId}",
                dto.BookingId, dto.Accept ? "Accepted" : "Rejected", ownerId);

            return ServiceResult.Success();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── GetEarningsAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<EarningsSummaryDto>> GetEarningsAsync(
        string ownerId,
        int year,
        CancellationToken ct = default)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = yearStart.AddYears(1).AddTicks(-1);

        var confirmedBookings = (await _uow.Bookings
            .GetConfirmedAsync(yearStart, yearEnd, ct))
            .Where(b => b.Property.OwnerId == ownerId)
            .ToList();

        var totalEarnings = await _uow.Owners.GetTotalEarningsAsync(ownerId, ct);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthlyBookings = confirmedBookings
            .Where(b => b.CreatedAt >= monthStart)
            .ToList();
        var monthlyEarnings = monthlyBookings
            .Sum(b => b.TotalAmount - (b.CommissionAmount ?? 0));

        var totalCommission = confirmedBookings
            .Sum(b => b.CommissionAmount ?? 0);

        // Per-property breakdown
        var ownerProperties = await _uow.Properties
            .GetByOwnerIdAsync(ownerId, false, ct);

        var byProperty = ownerProperties
            .Select(p =>
            {
                var propBookings = confirmedBookings
                    .Where(b => b.PropertyId == p.Id).ToList();
                var gross = propBookings.Sum(b => b.TotalAmount);
                var commission = propBookings.Sum(b => b.CommissionAmount ?? 0);
                return new PropertyEarningsRow
                {
                    PropertyId        = p.Id,
                    Title             = p.Title,
                    AreaNameEn        = p.Area.NameEn,
                    ConfirmedBookings = propBookings.Count,
                    GrossRevenue      = gross,
                    CommissionPaid    = commission,
                    NetEarnings       = gross - commission
                };
            })
            .OrderByDescending(r => r.NetEarnings)
            .ToList();

        // Monthly chart
        var monthlyBreakdown = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var mStart = new DateTime(year, month, 1);
                var mEnd = mStart.AddMonths(1).AddTicks(-1);
                var amount = confirmedBookings
                    .Where(b => b.CreatedAt >= mStart && b.CreatedAt <= mEnd)
                    .Sum(b => b.TotalAmount - (b.CommissionAmount ?? 0));

                return new MonthlyEarningsPoint
                {
                    MonthLabel = mStart.ToString("MMM"),
                    Amount = amount
                };
            })
            .ToList();

        return ServiceResult<EarningsSummaryDto>.Success(new EarningsSummaryDto
        {
            TotalEarnings          = totalEarnings,
            MonthlyEarnings        = monthlyEarnings,
            TotalCommissionPaid    = totalCommission,
            TotalConfirmedBookings = confirmedBookings.Count,
            ByProperty             = byProperty,
            MonthlyBreakdown       = monthlyBreakdown
        });
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<NotificationDto>>> GetNotificationsAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Notifications
            .GetByUserIdAsync(userId, pageNumber, pageSize, ct);

        var dtos = items.Select(MapToNotificationDto).ToList();

        return ServiceResult<PagedResult<NotificationDto>>.Success(
            PagedResult<NotificationDto>.Create(dtos, total, pageNumber, pageSize));
    }

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

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static BookingRequestDto MapToBookingRequestDto(Booking b) =>
        new()
        {
            BookingId          = b.Id,
            StudentFullName    = b.Student.FullName,
            StudentImageUrl    = b.Student.ProfileImageUrl,
            StudentEmail       = string.Empty,
            StudentIsVerified  = b.Student.IsIdentityVerified,
            PropertyId         = b.PropertyId,
            PropertyTitle      = b.Property.Title,
            PropertyImageUrl   = b.Property.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            AreaNameEn         = b.Property.Area.NameEn,
            CheckInDate        = b.CheckInDate,
            CheckOutDate       = b.CheckOutDate,
            TotalAmount        = b.TotalAmount,
            DepositAmount      = b.DepositAmount,
            Status             = b.Status,
            RequestedAt        = b.RequestedAt,
            RespondedAt        = b.RespondedAt
        };

    private static NotificationDto MapToNotificationDto(Notification n) =>
        new()
        {
            NotificationId    = n.NotificationId,
            Title             = n.Title,
            Message           = n.Message,
            Type              = n.Type,
            IsRead            = n.IsRead,
            RelatedEntityId   = n.RelatedEntityId,
            RelatedEntityType = n.RelatedEntityType,
            CreatedAt         = n.CreatedAt
        };

    private async Task<IReadOnlyList<MonthlyEarningsPoint>> BuildEarningsChartAsync(
        string ownerId, int months, CancellationToken ct)
    {
        var result = new List<MonthlyEarningsPoint>();
        var now = DateTime.UtcNow;

        for (int i = months - 1; i >= 0; i--)
        {
            var mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            var bookings = await _uow.Bookings.GetConfirmedAsync(mStart, mEnd, ct);
            var amount = bookings
                .Where(b => b.Property.OwnerId == ownerId)
                .Sum(b => b.TotalAmount - (b.CommissionAmount ?? 0));

            result.Add(new MonthlyEarningsPoint
            {
                MonthLabel = mStart.ToString("MMM yy"),
                Amount = amount
            });
        }

        return result;
    }
}
