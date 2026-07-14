// Skanly.Application/Features/Payments/Services/PaymentService.cs
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Payments.DTOs;
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;


namespace Skanly.Application.Features.Payments.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IValidator<InitiatePaymentDto> _validator;
    private readonly ILogger<PaymentService> _logger;
    private readonly INotificationService _notificationService;

    public PaymentService(
        IUnitOfWork uow,
        IPaymentGatewayFactory gatewayFactory,
        IValidator<InitiatePaymentDto> validator,
        ILogger<PaymentService> logger,
        INotificationService notificationService)
    {
        _uow = uow;
        _gatewayFactory = gatewayFactory;
        _validator = validator;
        _logger = logger;
        _notificationService = notificationService;
    }

    // ── GetCheckoutAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<CheckoutViewModel>> GetCheckoutAsync(
        string studentId,
        int bookingId,
        CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult<CheckoutViewModel>.Failure("Booking not found.");

        if (booking.StudentId != studentId)
            return ServiceResult<CheckoutViewModel>.Failure("Access denied.");

        // Only Accepted bookings can proceed to payment
        if (booking.Status != BookingStatus.Accepted &&
            booking.Status != BookingStatus.PaymentPending)
            return ServiceResult<CheckoutViewModel>.Failure(
                $"Payment is not available for a booking with status: {booking.Status}. " +
                "The owner must accept your request first.");

        // Check for an existing successful payment (idempotency guard)
        var alreadyPaid = await _uow.Repository<Payment>().ExistsAsync(
            p => p.BookingId == bookingId &&
                 p.Status == PaymentStatus.Success, ct);

        if (alreadyPaid)
            return ServiceResult<CheckoutViewModel>.Failure(
                "This booking has already been paid.");

        var vm = new CheckoutViewModel
        {
            BookingId = booking.Id,
            PropertyTitle = booking.Property.Title,
            PropertyImageUrl = booking.Property.Images
                                  .FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            OwnerFullName = booking.Property.Owner.FullName,
            AreaNameEn = booking.Property.Area.NameEn,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            TotalAmount = booking.TotalAmount,
            DepositAmount = booking.DepositAmount,
            CommissionRate = booking.CommissionRate,
            PaymentForm = new InitiatePaymentDto { BookingId = bookingId }
        };

        return ServiceResult<CheckoutViewModel>.Success(vm);
    }

    // ── ProcessPaymentAsync ───────────────────────────────────────────────────

    public async Task<ServiceResult<PaymentResultDto>> ProcessPaymentAsync(
        string studentId,
        InitiatePaymentDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate input
        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<PaymentResultDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Load and guard booking
        var booking = await _uow.Bookings.GetDetailAsync(dto.BookingId, ct);

        if (booking is null)
            return ServiceResult<PaymentResultDto>.Failure("Booking not found.");

        if (booking.StudentId != studentId)
            return ServiceResult<PaymentResultDto>.Failure("Access denied.");

        if (booking.Status != BookingStatus.Accepted &&
            booking.Status != BookingStatus.PaymentPending)
            return ServiceResult<PaymentResultDto>.Failure(
                "This booking is not awaiting payment.");

        // 3. Idempotency — reject if already successfully paid
        var alreadyPaid = await _uow.Repository<Payment>().ExistsAsync(
            p => p.BookingId == dto.BookingId &&
                 p.Status == PaymentStatus.Success, ct);

        if (alreadyPaid)
            return ServiceResult<PaymentResultDto>.Failure(
                "This booking has already been paid.");

        // 4. Mark booking as PaymentPending to prevent duplicate submissions
        if (booking.Status == BookingStatus.Accepted)
        {
            booking.Status = BookingStatus.PaymentPending;
            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);
        }

        // 5. Get gateway and build request
        IPaymentGateway gateway;
        try
        {
            gateway = _gatewayFactory.GetGateway(dto.PaymentMethod);
        }
        catch (NotSupportedException ex)
        {
            return ServiceResult<PaymentResultDto>.Failure(ex.Message);
        }

        var gatewayRequest = new GatewayRequest
        {
            BookingId = booking.Id,
            Amount = booking.DepositAmount,
            Currency = "EGP",
            CardNumber = dto.CardNumber?.Replace(" ", ""),
            CardHolderName = dto.CardHolderName,
            CardExpiry = dto.CardExpiry,
            CardCvv = dto.CardCvv,
            MobileNumber = dto.MobileNumber,
            Description = $"Deposit for booking #{booking.Id} - {booking.Property.Title}",
            Metadata = new Dictionary<string, string>
            {
                ["studentId"] = studentId,
                ["propertyId"] = booking.PropertyId.ToString(),
                ["ownerId"] = booking.Property.OwnerId
            }
        };

        // 6. Call gateway
        var gatewayResult = await gateway.ProcessAsync(gatewayRequest, ct);

        // 7. Persist payment record + update booking atomically
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var paymentStatus = gatewayResult.IsSuccess
                ? PaymentStatus.Success
                : PaymentStatus.Failed;

            var payment = new Payment
            {
                BookingId = booking.Id,
                PaymentMethod = dto.PaymentMethod,
                Amount = booking.DepositAmount,
                TransactionReference = gatewayResult.IsSuccess
                                        ? gatewayResult.TransactionReference
                                        : $"FAILED-{Guid.NewGuid():N[..8]}"
                                              .ToUpper(),
                Status = paymentStatus,
                PaidAt = gatewayResult.IsSuccess ? DateTime.UtcNow : null
            };

            await _uow.Repository<Payment>().AddAsync(payment, ct);

            if (gatewayResult.IsSuccess)
            {
                // Update booking to Confirmed
                booking.Status = BookingStatus.Confirmed;
                booking.CommissionAmount =
                    Math.Round(booking.TotalAmount * booking.CommissionRate / 100, 2);
                _uow.Bookings.Update(booking);

                // Notify student
                await _notificationService.SendPaymentSuccessAsync(
            studentId,
            booking.Id,
            booking.Property.Title,
            payment.Amount,
            gatewayResult.TransactionReference,
            ct);

                await _notificationService.SendOwnerPayoutNoticeAsync(
                    booking.Property.OwnerId,
                    booking.Id,
                    booking.Property.Title,
                    booking.TotalAmount - (booking.CommissionAmount ?? 0),
                    ct);
            }
            else
            {
                // Revert to Accepted so student can retry
                booking.Status = BookingStatus.Accepted;
                _uow.Bookings.Update(booking);

                // Notify student of failure
                await _notificationService.SendPaymentFailedAsync(
                    studentId,
                   booking.Id,
                     booking.Property.Title,
                     gatewayResult.FailureMessage ?? "Unknown error",
                          ct);
                                }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Payment {Status} for Booking {BookingId} via {Method}. Ref={Ref}",
                paymentStatus, booking.Id, dto.PaymentMethod,
                gatewayResult.TransactionReference);

            return gatewayResult.IsSuccess
                ? ServiceResult<PaymentResultDto>.Success(
                    PaymentResultDto.Success(
                        payment.Id,
                        booking.Id,
                        gatewayResult.TransactionReference,
                        payment.Amount))
                : ServiceResult<PaymentResultDto>.Success(
                    PaymentResultDto.Failure(
                        booking.Id,
                        gatewayResult.FailureMessage ?? "Payment failed."));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── GetStudentPaymentHistoryAsync ─────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<PaymentDto>>> GetStudentPaymentHistoryAsync(
        string studentId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Repository<Payment>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: p => p.Booking.StudentId == studentId,
            orderBy: q => q.OrderByDescending(p => p.CreatedAt),
            ct: ct,
            p => p.Booking,
            p => p.Booking.Property,
            p => p.Booking.Property.Images,
            p => p.Booking.Property.Area);

        var dtos = items.Select(MapToDto).ToList();

        return ServiceResult<PagedResult<PaymentDto>>.Success(
            PagedResult<PaymentDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── GetPaymentByIdAsync ───────────────────────────────────────────────────

    public async Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(
        string studentId,
        int paymentId,
        CancellationToken ct = default)
    {
        var payment = await _uow.Repository<Payment>()
            .GetFirstOrDefaultAsync(
                p => p.Id == paymentId &&
                     p.Booking.StudentId == studentId,
                ct,
                p => p.Booking,
                p => p.Booking.Property,
                p => p.Booking.Property.Images,
                p => p.Booking.Property.Area);

        if (payment is null)
            return ServiceResult<PaymentDto>.Failure("Payment not found.");

        return ServiceResult<PaymentDto>.Success(MapToDto(payment));
    }

    // ── GetAllPaymentsAsync (Admin) ───────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<PaymentHistoryDto>>> GetAllPaymentsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? statusFilter = null,
        string? methodFilter = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        PaymentStatus? status = null;
        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<PaymentStatus>(statusFilter, out var parsedStatus))
            status = parsedStatus;

        PaymentMethod? method = null;
        if (!string.IsNullOrEmpty(methodFilter) &&
            Enum.TryParse<PaymentMethod>(methodFilter, out var parsedMethod))
            method = parsedMethod;

        var (items, total) = await _uow.Repository<Payment>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: p =>
                (status == null || p.Status == status) &&
                (method == null || p.PaymentMethod == method) &&
                (from == null || p.CreatedAt >= from) &&
                (to == null || p.CreatedAt <= to),
            orderBy: q => q.OrderByDescending(p => p.CreatedAt),
            ct: ct,
            p => p.Booking,
            p => p.Booking.Student,
            p => p.Booking.Property,
            p => p.Booking.Property.Owner,
            p => p.Booking.Property.Images,
            p => p.Booking.Property.Area);

        var dtos = items.Select(MapToHistoryDto).ToList();

        return ServiceResult<PagedResult<PaymentHistoryDto>>.Success(
            PagedResult<PaymentHistoryDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── GetPaymentSummaryAsync (Admin) ────────────────────────────────────────

    public async Task<ServiceResult<PaymentSummaryDto>> GetPaymentSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var payments = await _uow.Repository<Payment>().GetAllAsync(
            p => p.CreatedAt >= from && p.CreatedAt <= to,
            ct);

        var successful = payments
            .Where(p => p.Status == PaymentStatus.Success)
            .ToList();

        var totalCollected = successful.Sum(p => p.Amount);
        var totalCommission = await _uow.Bookings.GetTotalCommissionAsync(from, to, ct);

        var byMethod = successful
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new MethodBreakdown
            {
                Method = g.Key.ToString(),
                MethodIcon = GetMethodIcon(g.Key),
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(m => m.Count)
            .ToList();

        var summary = new PaymentSummaryDto
        {
            TotalCollected = totalCollected,
            TotalCommission = totalCommission,
            TotalOwnerPayouts = totalCollected - totalCommission,
            TotalTransactions = payments.Count,
            SuccessfulTransactions = successful.Count,
            FailedTransactions = payments.Count(p => p.Status == PaymentStatus.Failed),
            ByMethod = byMethod
        };

        return ServiceResult<PaymentSummaryDto>.Success(summary);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static PaymentDto MapToDto(Payment p) => new()
    {
        PaymentId = p.Id,
        BookingId = p.BookingId,
        PropertyTitle = p.Booking.Property.Title,
        PropertyImageUrl = p.Booking.Property.Images
                                  .FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        PaymentMethod = p.PaymentMethod,
        Amount = p.Amount,
        TransactionReference = p.TransactionReference,
        Status = p.Status,
        PaidAt = p.PaidAt,
        CreatedAt = p.CreatedAt
    };

    private static PaymentHistoryDto MapToHistoryDto(Payment p) => new()
    {
        PaymentId = p.Id,
        BookingId = p.BookingId,
        StudentFullName = p.Booking.Student.FullName,
        OwnerFullName = p.Booking.Property.Owner.FullName,
        PropertyTitle = p.Booking.Property.Title,
        PaymentMethodDisplay = p.PaymentMethod.ToString(),
        PaymentMethodIcon = GetMethodIcon(p.PaymentMethod),
        Amount = p.Amount,
        CommissionAmount = p.Booking.CommissionAmount ?? 0,
        OwnerPayout = p.Amount - (p.Booking.CommissionAmount ?? 0),
        TransactionReference = p.TransactionReference,
        StatusDisplay = p.Status.ToString(),
        StatusBadgeClass = p.Status switch
        {
            PaymentStatus.Success => "bg-success",
            PaymentStatus.Failed => "bg-danger",
            _ => "bg-warning text-dark"
        },
        PaidAt = p.PaidAt,
        CreatedAt = p.CreatedAt
    };

    private static string GetMethodIcon(PaymentMethod method) => method switch
    {
        PaymentMethod.Visa => "fa-cc-visa",
        PaymentMethod.Mastercard => "fa-cc-mastercard",
        PaymentMethod.VodafoneCash => "fa-mobile-alt",
        PaymentMethod.InstaPay => "fa-bolt",
        PaymentMethod.Fawry => "fa-store",
        _ => "fa-credit-card"
    };
}