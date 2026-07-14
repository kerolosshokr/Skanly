// Skanly.Infrastructure/Pdf/QuestPdfContractService.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Contracts.DTOs;
using Skanly.Application.Features.Contracts.Interfaces;
using Skanly.Application.Features.Contracts.Services;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;
using Skanly.Infrastructure.Identity;

namespace Skanly.Infrastructure.Pdf;

public class QuestPdfContractService : IPdfContractService
{
    private readonly IUnitOfWork _uow;
    private readonly ContractSettings _settings;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<QuestPdfContractService> _logger;

    public QuestPdfContractService(
        IUnitOfWork uow,
        IOptions<ContractSettings> settings,
        IWebHostEnvironment env,
        ILogger<QuestPdfContractService> logger)
    {
        _uow = uow;
        _settings = settings.Value;
        _env = env;
        _logger = logger;
    }

    // ── GenerateForBookingAsync ───────────────────────────────────────────────

    public async Task<ServiceResult<ContractDto>> GenerateForBookingAsync(
        int bookingId,
        CancellationToken ct = default)
    {
        // 1. Load booking with all navigations
        var booking = await _uow.Bookings.GetDetailAsync(bookingId, ct);

        if (booking is null)
            return ServiceResult<ContractDto>.Failure("Booking not found.");

        if (booking.Status != BookingStatus.Confirmed)
            return ServiceResult<ContractDto>.Failure(
                "Contracts can only be generated for Confirmed bookings.");

        // 2. Check if contract already exists
        var existing = await _uow.Repository<Contract>()
            .GetFirstOrDefaultAsync(
                c => c.BookingId == bookingId, ct);

        if (existing is not null)
        {
            var existingDto = await BuildContractDtoAsync(existing, ct);
            return ServiceResult<ContractDto>.Success(existingDto);
        }

        // 3. Load additional data
        var student = await _uow.Students
            .GetByUserIdAsync(booking.StudentId, ct);

        var studentEmail = await _uow.Repository<ApplicationUser>()
            .GetFirstOrDefaultAsync(
                u => u.Id == booking.StudentId, ct)
            .ContinueWith(t => t.Result?.Email ?? "", ct);

        var owner = await _uow.Owners
            .GetByUserIdAsync(booking.Property.OwnerId, ct);

        var ownerEmail = await _uow.Repository<ApplicationUser>()
            .GetFirstOrDefaultAsync(
                u => u.Id == booking.Property.OwnerId, ct)
            .ContinueWith(t => t.Result?.Email ?? "", ct);

        // Load the successful payment for transaction reference
        var payment = await _uow.Repository<Payment>()
            .GetFirstOrDefaultAsync(
                p => p.BookingId == bookingId &&
                     p.Status == PaymentStatus.Success, ct);

        // 4. Generate contract number
        var generatedAt = DateTime.UtcNow;
        var contractNumber = ContractNumberGenerator
            .Generate(bookingId, generatedAt);

        // 5. Build data model
        var data = new GenerateContractDto
        {
            BookingId = bookingId,
            ContractNumber = contractNumber,
            GeneratedAt = generatedAt,

            // Student
            StudentFullName = student?.FullName ?? "Unknown",
            StudentNationalId = student?.NationalId ?? "—",
            StudentPhone = student?.PhoneNumber,
            StudentEmail = studentEmail,
            StudentUniversityNameEn = booking.Property.University?.NameEn,

            // Owner
            OwnerFullName = owner?.FullName ?? "Unknown",
            OwnerNationalId = owner?.NationalId ?? "—",
            OwnerPhone = owner?.PhoneNumber,
            OwnerEmail = ownerEmail,
            OwnerBusinessName = owner?.BusinessName,

            // Property
            PropertyId = booking.PropertyId,
            PropertyTitle = booking.Property.Title,
            PropertyAddress = booking.Property.Address ?? string.Empty,
            PropertyTypeDisplay = booking.Property.PropertyType.ToString(),
            AreaNameEn = booking.Property.Area?.NameEn ?? "",
            UniversityNameEn = booking.Property.University?.NameEn,
            Rooms = booking.Property.Rooms,
            Beds = booking.Property.Beds,
            AmenityNames = booking.Property.PropertyAmenities
                                    .Select(a => a.Amenity.NameEn)
                                    .ToList(),

            // Financial
            MonthlyRent = booking.Property.PricePerMonth,
            DepositAmount = booking.DepositAmount,
            CommissionRate = booking.CommissionRate,
            CommissionAmount = booking.CommissionAmount ?? 0,
            PaymentMethod = payment?.PaymentMethod.ToString() ?? "—",
            TransactionReference = payment?.TransactionReference,

            // Dates
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,

            // Platform
            PlatformNameEn = _settings.CompanyNameEn,
            PlatformNameAr = _settings.CompanyNameAr
        };

        // 6. Generate PDF bytes
        byte[] pdfBytes;
        try
        {
            var document = new ContractDocument(data, _settings);
            pdfBytes = document.GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PDF generation failed for booking {BookingId}", bookingId);
            return ServiceResult<ContractDto>.Failure(
                "PDF generation failed. Please try again.");
        }

        // 7. Save PDF file
        var fileName = $"{contractNumber}.pdf";
        var directory = Path.Combine(
            _env.WebRootPath, _settings.StoragePath);

        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(filePath, pdfBytes, ct);

        var pdfUrl = $"/{_settings.StoragePath}/{fileName}";

        // 8. Persist Contract entity
        var contract = new Contract
        {
            BookingId = bookingId,
            ContractNumber = contractNumber,
            PdfUrl = pdfUrl,
            GeneratedAt = generatedAt
        };

        await _uow.Repository<Contract>().AddAsync(contract, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contract {ContractNumber} generated for Booking {BookingId}. " +
            "Size={Size}KB",
            contractNumber, bookingId, pdfBytes.Length / 1024);

        return ServiceResult<ContractDto>.Success(
            await BuildContractDtoAsync(contract, ct));
    }

    // ── GetByBookingAsync ─────────────────────────────────────────────────────

    public async Task<ServiceResult<ContractDto>> GetByBookingAsync(
        string requesterId,
        int bookingId,
        bool isAdmin = false,
        CancellationToken ct = default)
    {
        var contract = await _uow.Repository<Contract>()
            .GetFirstOrDefaultAsync(
                c => c.BookingId == bookingId, ct);

        if (contract is null)
            return ServiceResult<ContractDto>.Failure(
                "Contract not found for this booking.");

        // Access guard — must be student, owner, or Admin
        if (!isAdmin)
        {
            var booking = await _uow.Bookings
                .GetDetailAsync(bookingId, ct);

            if (booking is null)
                return ServiceResult<ContractDto>.Failure("Booking not found.");

            var isStudent = booking.StudentId == requesterId;
            var isOwner = booking.Property.OwnerId == requesterId;

            if (!isStudent && !isOwner)
                return ServiceResult<ContractDto>.Failure("Access denied.");
        }

        return ServiceResult<ContractDto>.Success(
            await BuildContractDtoAsync(contract, ct));
    }

    // ── GetPdfBytesAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<byte[]>> GetPdfBytesAsync(
        string requesterId,
        int bookingId,
        bool isAdmin = false,
        CancellationToken ct = default)
    {
        var result = await GetByBookingAsync(
            requesterId, bookingId, isAdmin, ct);

        if (!result.IsSuccess)
            return ServiceResult<byte[]>.Failure(result.ErrorMessage!);

        var contract = result.Data!;
        var filePath = Path.Combine(
            _env.WebRootPath,
            contract.PdfUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(filePath))
            return ServiceResult<byte[]>.Failure(
                "Contract PDF file not found. Please regenerate.");

        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return ServiceResult<byte[]>.Success(bytes);
    }

    // ── RegenerateAsync (Admin) ───────────────────────────────────────────────

    public async Task<ServiceResult<ContractDto>> RegenerateAsync(
        int bookingId,
        CancellationToken ct = default)
    {
        // Remove existing contract record (file will be overwritten)
        var existing = await _uow.Repository<Contract>()
            .GetFirstOrDefaultAsync(c => c.BookingId == bookingId, ct);

        if (existing is not null)
        {
            // Delete old PDF file
            var oldPath = Path.Combine(
                _env.WebRootPath,
                existing.PdfUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(oldPath))
                File.Delete(oldPath);

            _uow.Repository<Contract>().Remove(existing);
            await _uow.SaveChangesAsync(ct);
        }

        // Generate fresh contract
        return await GenerateForBookingAsync(bookingId, ct);
    }

    // ── Private Helper ────────────────────────────────────────────────────────

    private async Task<ContractDto> BuildContractDtoAsync(
        Contract c,
        CancellationToken ct)
    {
        var booking = await _uow.Bookings.GetDetailAsync(c.BookingId, ct);
        var student = booking is not null
            ? await _uow.Students.GetByUserIdAsync(booking.StudentId, ct)
            : null;
        var owner = booking is not null
            ? await _uow.Owners.GetByUserIdAsync(booking.Property.OwnerId, ct)
            : null;

        return new ContractDto
        {
            ContractId = c.Id,
            BookingId = c.BookingId,
            ContractNumber = c.ContractNumber,
            PdfUrl = c.PdfUrl,
            GeneratedAt = c.GeneratedAt,
            StudentFullName = student?.FullName ?? "",
            StudentNationalId = student?.NationalId ?? "",
            StudentPhone = student?.PhoneNumber,
            OwnerFullName = owner?.FullName ?? "",
            OwnerNationalId = owner?.NationalId ?? "",
            OwnerPhone = owner?.PhoneNumber,
            PropertyTitle = booking?.Property.Title ?? "",
            PropertyAddress = booking?.Property.Address ?? "",
            PropertyTypeDisplay = booking?.Property.PropertyType.ToString() ?? "",
            MonthlyRent = booking?.Property.PricePerMonth ?? 0,
            DepositAmount = booking?.DepositAmount ?? 0,
            CommissionRate = booking?.CommissionRate ?? 0,
            CheckInDate = booking?.CheckInDate ?? DateOnly.MinValue,
            CheckOutDate = booking?.CheckOutDate
        };
    }
}