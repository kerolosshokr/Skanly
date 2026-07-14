// Skanly.Application/Features/Bookings/Services/BookingService.cs
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Bookings.DTOs;
using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Application.Features.Contracts.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Features.Bookings.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateBookingDto> _createValidator;
    private readonly IValidator<CancelBookingDto> _cancelValidator;
    private readonly ILogger<BookingService> _logger;
    private readonly IPdfContractService _contractService;


    // Deposit percentage (e.g. 20% of monthly rent as deposit)
    private const decimal DepositPercentage = 0.20m;

    public BookingService(
        IUnitOfWork uow,
        INotificationService notificationService,
         IPdfContractService contractService,
        IValidator<CreateBookingDto> createValidator,
        IValidator<CancelBookingDto> cancelValidator,
        ILogger<BookingService> logger)
    {
        _uow = uow;
        _notificationService = notificationService;
        _createValidator = createValidator;
        _contractService = contractService;
        _cancelValidator = cancelValidator;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STUDENT OPERATIONS
    // ══════════════════════════════════════════════════════════════════════════

    // ── CreateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<BookingDto>> CreateAsync(
        string studentId,
        CreateBookingDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate input
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<BookingDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Load student and enforce identity verification
        var student = await _uow.Students.GetByUserIdAsync(studentId, ct);
        if (student is null)
            return ServiceResult<BookingDto>.Failure("Student profile not found.");

        if (!student.IsIdentityVerified)
            return ServiceResult<BookingDto>.Failure(
                "You must verify your identity before making a booking. " +
                "Please submit your National ID for verification.");

        // 3. Load property
        var property = await _uow.Properties.GetDetailAsync(dto.PropertyId, ct);
        if (property is null || property.IsDeleted)
            return ServiceResult<BookingDto>.Failure("Property not found.");

        if (!property.IsApproved)
            return ServiceResult<BookingDto>.Failure(
                "This property is not yet approved for bookings.");

        if (!property.IsAvailable)
            return ServiceResult<BookingDto>.Failure(
                "This property is not currently available.");

        // 4. Student cannot book their own property
        //    (guard against edge case where student is also an owner)
        if (property.OwnerId == studentId)
            return ServiceResult<BookingDto>.Failure(
                "You cannot book your own property.");

        // 5. Check for overlapping active bookings on this property
        var isAvailable = await _uow.Bookings.IsPropertyAvailableAsync(
            dto.PropertyId, dto.CheckInDate, dto.CheckOutDate,
            excludeBookingId: null, ct);

        if (!isAvailable)
            return ServiceResult<BookingDto>.Failure(
                "This property is already booked for the selected dates. " +
                "Please choose different dates or another property.");

        // 6. Student must not already have an open booking for this property
        var existingOpen = await _uow.Bookings.ExistsAsync(
            b => b.StudentId == studentId &&
                 b.PropertyId == dto.PropertyId &&
                 (b.Status == BookingStatus.Pending ||
                  b.Status == BookingStatus.Accepted ||
                  b.Status == BookingStatus.PaymentPending), ct);

        if (existingOpen)
            return ServiceResult<BookingDto>.Failure(
                "You already have an open booking request for this property. " +
                "Please wait for the owner's response or cancel your existing request.");

        // 7. Get active commission rate
        var commissionSetting = await _uow.Repository<CommissionSetting>()
            .GetFirstOrDefaultAsync(c => c.IsActive, ct);

        var commissionRate = commissionSetting?.Rate ?? 10.00m;

        // 8. Calculate amounts
        // For open-ended bookings, TotalAmount = first month's rent
        // For date-range bookings, calculate pro-rated
        var totalAmount = CalculateTotalAmount(
            property.PricePerMonth, dto.CheckInDate, dto.CheckOutDate);
        var depositAmount = Math.Round(totalAmount * DepositPercentage, 2);

        // 9. Create booking
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var booking = new Booking
            {
                StudentId = studentId,
                PropertyId = dto.PropertyId,
                Status = BookingStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                CommissionRate = commissionRate
            };

            await _uow.Bookings.AddAsync(booking, ct);
            await _uow.SaveChangesAsync(ct);

            // 10. Notify owner
            await _notificationService.SendBookingReceivedAsync(
                ownerId: property.OwnerId,
                bookingId: booking.Id,
                propertyTitle: property.Title,
                studentName: student.FullName,
                ct: ct);

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Booking {BookingId} created by Student {StudentId} " +
                "for Property {PropertyId}",
                booking.Id, studentId, dto.PropertyId);

            return ServiceResult<BookingDto>.Success(
                await MapToDto(booking, ct));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult> CancelAsync(
        string studentId,
        CancelBookingDto dto,
        CancellationToken ct = default)
    {
        var validation = await _cancelValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        var booking = await _uow.Bookings.GetDetailAsync(dto.BookingId, ct);

        if (booking is null)
            return ServiceResult.Failure("Booking not found.");

        if (booking.StudentId != studentId)
            return ServiceResult.Failure("Access denied.");

        // State machine guard
        if (booking.Status != BookingStatus.Pending &&
            booking.Status != BookingStatus.Accepted)
            return ServiceResult.Failure(
                $"A {booking.StatusDisplay()} booking cannot be cancelled. " +
                "Only Pending or Accepted bookings can be cancelled.");

        var wasAccepted = booking.Status == BookingStatus.Accepted;
        var propertyTitle = booking.Property.Title;
        var ownerId = booking.Property.OwnerId;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            booking.Status = BookingStatus.Cancelled;
            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);

            // Notify owner only if booking was already accepted
            if (wasAccepted)
            {
                await _notificationService.SendBookingCancelledAsync(
                    recipientId: ownerId,
                    bookingId: booking.Id,
                    propertyTitle: propertyTitle,
                    cancelledBy: "the student",
                    ct: ct);
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Booking {BookingId} cancelled by Student {StudentId}. " +
                "Reason: {Reason}",
                dto.BookingId, studentId, dto.Reason);

            return ServiceResult.Success();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── GetByStudentAsync ─────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<BookingDto>>>
        GetByStudentAsync(
            string studentId,
            int pageNumber = 1,
            int pageSize = 10,
            BookingStatus? statusFilter = null,
            CancellationToken ct = default)
    {
        var (items, total) = await _uow.Bookings.GetByStudentIdAsync(
            studentId, pageNumber, pageSize, statusFilter, ct);

        var dtos = new List<BookingDto>();
        foreach (var b in items)
            dtos.Add(await MapToDto(b, ct));

        return ServiceResult<PagedResult<BookingDto>>.Success(
            PagedResult<BookingDto>.Create(
                dtos, total, pageNumber, pageSize));
    }

    // ── GetDetailForStudentAsync ──────────────────────────────────────────────

    public async Task<ServiceResult<BookingDetailDto>>
        GetDetailForStudentAsync(
            string studentId,
            int bookingId,
            CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult<BookingDetailDto>.Failure(
                "Booking not found.");

        if (booking.StudentId != studentId)
            return ServiceResult<BookingDetailDto>.Failure("Access denied.");

        return ServiceResult<BookingDetailDto>.Success(
            await MapToDetailDto(booking, ct));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // OWNER OPERATIONS
    // ══════════════════════════════════════════════════════════════════════════

    // ── AcceptAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult> AcceptAsync(
        string ownerId,
        int bookingId,
        CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult.Failure("Booking not found.");

        // Ownership guard
        if (booking.Property.OwnerId != ownerId)
            return ServiceResult.Failure("Access denied.");

        // State machine guard
        if (booking.Status != BookingStatus.Pending)
            return ServiceResult.Failure(
                $"Cannot accept a booking with status: {booking.StatusDisplay()}. " +
                "Only Pending bookings can be accepted.");

        // Re-check availability (another booking may have been confirmed since)
        var stillAvailable = await _uow.Bookings.IsPropertyAvailableAsync(
            booking.PropertyId, booking.CheckInDate, booking.CheckOutDate,
            excludeBookingId: bookingId, ct);

        if (!stillAvailable)
            return ServiceResult.Failure(
                "This property is no longer available for the requested dates. " +
                "Another booking may have been confirmed in the meantime.");

        // Snapshot commission rate at acceptance time
        var commissionSetting = await _uow.Repository<CommissionSetting>()
            .GetFirstOrDefaultAsync(c => c.IsActive, ct);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            booking.Status = BookingStatus.Accepted;
            booking.RespondedAt = DateTime.UtcNow;
            booking.CommissionRate = commissionSetting?.Rate ?? booking.CommissionRate;

            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);

            // Notify student
            await _notificationService.SendBookingAcceptedAsync(
                studentId: booking.StudentId,
                bookingId: booking.Id,
                propertyTitle: booking.Property.Title,
                ct: ct);

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Booking {BookingId} accepted by Owner {OwnerId}",
                bookingId, ownerId);

            return ServiceResult.Success();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── RejectAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult> RejectAsync(
        string ownerId,
        int bookingId,
        string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ServiceResult.Failure(
                "Please provide a reason for rejection.");

        if (reason.Length > 500)
            return ServiceResult.Failure(
                "Rejection reason cannot exceed 500 characters.");

        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult.Failure("Booking not found.");

        if (booking.Property.OwnerId != ownerId)
            return ServiceResult.Failure("Access denied.");

        if (booking.Status != BookingStatus.Pending)
            return ServiceResult.Failure(
                $"Cannot reject a booking with status: {booking.StatusDisplay()}. " +
                "Only Pending bookings can be rejected.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            booking.Status = BookingStatus.Rejected;
            booking.RespondedAt = DateTime.UtcNow;

            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);

            await _notificationService.SendBookingRejectedAsync(
                studentId: booking.StudentId,
                bookingId: booking.Id,
                propertyTitle: booking.Property.Title,
                reason: reason,
                ct: ct);

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Booking {BookingId} rejected by Owner {OwnerId}. " +
                "Reason: {Reason}",
                bookingId, ownerId, reason);

            return ServiceResult.Success();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── GetByOwnerAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<BookingDto>>>
        GetByOwnerAsync(
            string ownerId,
            int pageNumber = 1,
            int pageSize = 10,
            BookingStatus? statusFilter = null,
            CancellationToken ct = default)
    {
        // Get all owner property IDs
        var ownerPropertyIds = (await _uow.Properties
            .GetByOwnerIdAsync(ownerId, false, ct))
            .Select(p => p.Id)
            .ToHashSet();

        var (items, total) = await _uow.Repository<Booking>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: b =>
                ownerPropertyIds.Contains(b.PropertyId) &&
                (statusFilter == null || b.Status == statusFilter),
            orderBy: q => q.OrderByDescending(b => b.RequestedAt),
            ct: ct,
            b => b.Student,
            b => b.Property,
            b => b.Property.Area,
            b => b.Property.Images);

        var dtos = new List<BookingDto>();
        foreach (var b in items)
            dtos.Add(await MapToDto(b, ct));

        return ServiceResult<PagedResult<BookingDto>>.Success(
            PagedResult<BookingDto>.Create(
                dtos, total, pageNumber, pageSize));
    }

    // ── GetDetailForOwnerAsync ────────────────────────────────────────────────

    public async Task<ServiceResult<BookingDetailDto>>
        GetDetailForOwnerAsync(
            string ownerId,
            int bookingId,
            CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult<BookingDetailDto>.Failure(
                "Booking not found.");

        if (booking.Property.OwnerId != ownerId)
            return ServiceResult<BookingDetailDto>.Failure("Access denied.");

        return ServiceResult<BookingDetailDto>.Success(
            await MapToDetailDto(booking, ct));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SYSTEM OPERATIONS
    // ══════════════════════════════════════════════════════════════════════════

    // ── MarkPaymentPendingAsync ───────────────────────────────────────────────

    public async Task<ServiceResult> MarkPaymentPendingAsync(
        string studentId,
        int bookingId,
        CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult.Failure("Booking not found.");

        if (booking.StudentId != studentId)
            return ServiceResult.Failure("Access denied.");

        if (booking.Status != BookingStatus.Accepted)
            return ServiceResult.Failure(
                "Payment can only be initiated for Accepted bookings.");

        booking.Status = BookingStatus.PaymentPending;
        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    // ── ConfirmAfterPaymentAsync ──────────────────────────────────────────────

    public async Task<ServiceResult> ConfirmAfterPaymentAsync(
        int bookingId,
        string transactionReference,
        CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult.Failure("Booking not found.");

        if (booking.Status != BookingStatus.Accepted &&
            booking.Status != BookingStatus.PaymentPending)
            return ServiceResult.Failure(
                "Booking is not in a payable state.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // Calculate commission
            booking.Status = BookingStatus.Confirmed;
            booking.CommissionAmount = Math.Round(
                booking.TotalAmount * booking.CommissionRate / 100, 2);

            _uow.Bookings.Update(booking);

            // Mark property as occupied
            booking.Property.IsAvailable = false;
            _uow.Repository<Property>().Update(booking.Property);

            await _uow.SaveChangesAsync(ct);

            // Notify both parties
            await _notificationService.SendBookingConfirmedAsync(
                studentId: booking.StudentId,
                ownerId: booking.Property.OwnerId,
                bookingId: booking.Id,
                propertyTitle: booking.Property.Title,
                ct: ct);

            // Notify owner of payout
            var netPayout = booking.TotalAmount -
                            (booking.CommissionAmount ?? 0);
            await _notificationService.SendOwnerPayoutNoticeAsync(
                ownerId: booking.Property.OwnerId,
                bookingId: booking.Id,
                propertyTitle: booking.Property.Title,
                netAmount: netPayout,
                ct: ct);

            await tx.CommitAsync(ct);
            await tx.CommitAsync(ct);

            // Generate contract asynchronously after commit
            // (non-blocking — failure is logged but doesn't affect booking status)
            _ = Task.Run(async () =>
            {
                try
                {
                    var contractResult = await _contractService
                        .GenerateForBookingAsync(bookingId);

                    if (!contractResult.IsSuccess)
                        _logger.LogWarning(
                            "Contract generation failed for Booking {BookingId}: {Error}",
                            bookingId, contractResult.ErrorMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception generating contract for Booking {BookingId}", bookingId);
                }
            }, CancellationToken.None);

            _logger.LogInformation(
                "Booking {BookingId} confirmed. TxRef={TxRef} " +
                "Commission={Commission}",
                bookingId, transactionReference,
                booking.CommissionAmount);

            return ServiceResult.Success();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ADMIN OPERATIONS
    // ══════════════════════════════════════════════════════════════════════════

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<BookingDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        BookingStatus? statusFilter = null,
        string? searchTerm = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Repository<Booking>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: b =>
                (statusFilter == null || b.Status == statusFilter) &&
                (from == null || b.CreatedAt >= from) &&
                (to == null || b.CreatedAt <= to) &&
                (searchTerm == null ||
                 b.Property.Title.Contains(searchTerm) ||
                 b.Student.FirstName.Contains(searchTerm) ||
                 b.Student.LastName.Contains(searchTerm)),
            orderBy: q => q.OrderByDescending(b => b.CreatedAt),
            ct: ct,
            b => b.Student,
            b => b.Property,
            b => b.Property.Area,
            b => b.Property.Owner,
            b => b.Property.Images);

        var dtos = new List<BookingDto>();
        foreach (var b in items)
            dtos.Add(await MapToDto(b, ct));

        return ServiceResult<PagedResult<BookingDto>>.Success(
            PagedResult<BookingDto>.Create(
                dtos, total, pageNumber, pageSize));
    }

    // ── GetDetailForAdminAsync ────────────────────────────────────────────────

    public async Task<ServiceResult<BookingDetailDto>> GetDetailForAdminAsync(
        int bookingId,
        CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult<BookingDetailDto>.Failure(
                "Booking not found.");

        return ServiceResult<BookingDetailDto>.Success(
            await MapToDetailDto(booking, ct));
    }

    // ── GetStatsAsync ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<BookingStatsDto>> GetStatsAsync(
        CancellationToken ct = default)
    {
        var all = await _uow.Repository<Booking>().GetAllAsync(ct);

        var confirmed = all
            .Where(b => b.Status == BookingStatus.Confirmed)
            .ToList();

        var totalRevenue = confirmed.Sum(b => b.TotalAmount);
        var totalCommission = confirmed.Sum(b => b.CommissionAmount ?? 0);

        // Monthly trend — last 12 months
        var now = DateTime.UtcNow;
        var monthlyTrend = Enumerable.Range(0, 12)
            .Select(i =>
            {
                var mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var mEnd = mStart.AddMonths(1).AddTicks(-1);

                var monthBookings = all
                    .Where(b => b.CreatedAt >= mStart &&
                                b.CreatedAt <= mEnd)
                    .ToList();

                return new MonthlyBookingPoint
                {
                    MonthLabel = mStart.ToString("MMM yy"),
                    Count = monthBookings.Count,
                    Revenue = monthBookings
                                    .Where(b => b.Status == BookingStatus.Confirmed)
                                    .Sum(b => b.TotalAmount)
                };
            })
            .Reverse()
            .ToList();

        var stats = new BookingStatsDto
        {
            TotalBookings = all.Count,
            PendingBookings = all.Count(b => b.Status == BookingStatus.Pending),
            ConfirmedBookings = confirmed.Count,
            CancelledBookings = all.Count(b => b.Status == BookingStatus.Cancelled),
            RejectedBookings = all.Count(b => b.Status == BookingStatus.Rejected),
            TotalRevenue = totalRevenue,
            TotalCommission = totalCommission,
            MonthlyTrend = monthlyTrend
        };

        return ServiceResult<BookingStatsDto>.Success(stats);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculates total amount based on property price and date range.
    /// For open-ended bookings: first month's rent.
    /// For date-range bookings: pro-rated by days.
    /// </summary>
    private static decimal CalculateTotalAmount(
        decimal pricePerMonth,
        DateOnly checkIn,
        DateOnly? checkOut)
    {
        if (!checkOut.HasValue)
            return pricePerMonth; // One month deposit for open-ended

        var days = checkOut.Value.DayNumber - checkIn.DayNumber;
        var months = days / 30.0m;
        return Math.Round(pricePerMonth * months, 2);
    }

    private async Task<BookingDto> MapToDto(
        Booking b,
        CancellationToken ct)
    {
        var hasContract = await _uow.Repository<Contract>()
            .ExistsAsync(c => c.BookingId == b.Id, ct);

        var hasReview = await _uow.Repository<Review>()
            .ExistsAsync(r => r.BookingId == b.Id, ct);

        var hasPayment = await _uow.Repository<Payment>()
            .ExistsAsync(p => p.BookingId == b.Id &&
                              p.Status == PaymentStatus.Success, ct);

        return new BookingDto
        {
            BookingId = b.Id,
            StudentId = b.StudentId,
            StudentFullName = b.Student?.FullName ?? string.Empty,
            StudentImageUrl = b.Student?.ProfileImageUrl,
            StudentIsVerified = b.Student?.IsIdentityVerified ?? false,
            PropertyId = b.PropertyId,
            PropertyTitle = b.Property?.Title ?? string.Empty,
            PropertyImageUrl = b.Property?.Images
                                    .FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            AreaNameEn = b.Property?.Area.NameEn ?? string.Empty,
            UniversityNameEn = b.Property?.University?.NameEn,
            PropertyTypeDisplay = b.Property?.PropertyType.ToString() ?? string.Empty,
            OwnerId = b.Property?.OwnerId ?? string.Empty,
            OwnerFullName = b.Property?.Owner?.FullName ?? string.Empty,
            Status = b.Status,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            TotalAmount = b.TotalAmount,
            DepositAmount = b.DepositAmount,
            CommissionRate = b.CommissionRate,
            CommissionAmount = b.CommissionAmount,
            RequestedAt = b.RequestedAt,
            RespondedAt = b.RespondedAt,
            CreatedAt = b.CreatedAt,
            HasContract = hasContract,
            HasReview = hasReview,
            HasSuccessfulPayment = hasPayment
        };
    }

    private async Task<BookingDetailDto> MapToDetailDto(
        Booking b,
        CancellationToken ct)
    {
        var baseDto = await MapToDto(b, ct);

        // Payments
        var payments = await _uow.Repository<Payment>()
            .GetAllAsync(p => p.BookingId == b.Id, ct);

        var paymentDtos = payments.Select(p => new PaymentInfoDto
        {
            PaymentId = p.Id,
            MethodDisplay = p.PaymentMethod.ToString(),
            Amount = p.Amount,
            TransactionReference = p.TransactionReference,
            StatusDisplay = p.Status.ToString(),
            StatusBadgeClass = p.Status switch
            {
                PaymentStatus.Success => "bg-success",
                PaymentStatus.Failed => "bg-danger",
                _ => "bg-warning text-dark"
            },
            PaidAt = p.PaidAt
        }).ToList();

        // Contract
        var contract = await _uow.Repository<Contract>()
            .GetFirstOrDefaultAsync(c => c.BookingId == b.Id, ct);

        ContractInfoDto? contractDto = contract is null ? null : new()
        {
            ContractId = contract.Id,
            ContractNumber = contract.ContractNumber,
            PdfUrl = contract.PdfUrl,
            GeneratedAt = contract.GeneratedAt
        };

        // Review
        var review = await _uow.Repository<Review>()
            .GetFirstOrDefaultAsync(r => r.BookingId == b.Id, ct);

        ReviewInfoDto? reviewDto = review is null ? null : new()
        {
            ReviewId = review.Id,
            OverallRating = review.OverallRating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };

        return new BookingDetailDto
        {
            // Copy base fields
            BookingId = baseDto.BookingId,
            StudentId = baseDto.StudentId,
            StudentFullName = baseDto.StudentFullName,
            StudentImageUrl = baseDto.StudentImageUrl,
            StudentIsVerified = baseDto.StudentIsVerified,
            PropertyId = baseDto.PropertyId,
            PropertyTitle = baseDto.PropertyTitle,
            PropertyImageUrl = baseDto.PropertyImageUrl,
            AreaNameEn = baseDto.AreaNameEn,
            UniversityNameEn = baseDto.UniversityNameEn,
            PropertyTypeDisplay = baseDto.PropertyTypeDisplay,
            OwnerId = baseDto.OwnerId,
            OwnerFullName = baseDto.OwnerFullName,
            Status = baseDto.Status,
            CheckInDate = baseDto.CheckInDate,
            CheckOutDate = baseDto.CheckOutDate,
            TotalAmount = baseDto.TotalAmount,
            DepositAmount = baseDto.DepositAmount,
            CommissionRate = baseDto.CommissionRate,
            CommissionAmount = baseDto.CommissionAmount,
            RequestedAt = baseDto.RequestedAt,
            RespondedAt = baseDto.RespondedAt,
            CreatedAt = baseDto.CreatedAt,
            HasContract = baseDto.HasContract,
            HasReview = baseDto.HasReview,
            HasSuccessfulPayment = baseDto.HasSuccessfulPayment,
            // Detail-specific
            Payments = paymentDtos,
            Contract = contractDto,
            Review = reviewDto
        };
    }
}

/// <summary>Extension method to avoid switch on Status enum in student-facing messages.</summary>
internal static class BookingExtensions
{
    internal static string StatusDisplay(this Booking b) => b.Status switch
    {
        BookingStatus.Pending => "Pending",
        BookingStatus.Accepted => "Accepted",
        BookingStatus.Rejected => "Rejected",
        BookingStatus.PaymentPending => "Payment Pending",
        BookingStatus.Confirmed => "Confirmed",
        BookingStatus.Cancelled => "Cancelled",
        _ => b.Status.ToString()
    };
}