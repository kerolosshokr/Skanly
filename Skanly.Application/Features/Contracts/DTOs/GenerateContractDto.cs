// Skanly.Application/Features/Contracts/DTOs/GenerateContractDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Contracts.DTOs;

/// <summary>
/// All data needed to generate a contract PDF.
/// Built by IPdfContractService from the Booking entity.
/// </summary>
public class GenerateContractDto
{
    // ── Identifiers ────────────────────────────────────────────────────────────
    public int BookingId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // ── Student ────────────────────────────────────────────────────────────────
    public string StudentFullName { get; set; } = string.Empty;
    public string? StudentFullNameAr { get; set; }
    public string StudentNationalId { get; set; } = string.Empty;
    public string? StudentPhone { get; set; }
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentUniversityNameEn { get; set; }

    // ── Owner ──────────────────────────────────────────────────────────────────
    public string OwnerFullName { get; set; } = string.Empty;
    public string? OwnerFullNameAr { get; set; }
    public string OwnerNationalId { get; set; } = string.Empty;
    public string? OwnerPhone { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string? OwnerBusinessName { get; set; }

    // ── Property ───────────────────────────────────────────────────────────────
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string PropertyTypeDisplay { get; set; } = string.Empty;
    public string AreaNameEn { get; set; } = string.Empty;
    public string? UniversityNameEn { get; set; }
    public int Rooms { get; set; }
    public int Beds { get; set; }
    public string GenderPolicyDisplay { get; set; } = string.Empty;
    public IReadOnlyList<string> AmenityNames { get; set; }
        = new List<string>();

    // ── Financial ──────────────────────────────────────────────────────────────
    public decimal MonthlyRent { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }

    // ── Booking Dates ──────────────────────────────────────────────────────────
    public DateOnly CheckInDate { get; set; }
    public DateOnly? CheckOutDate { get; set; }

    // ── Platform ───────────────────────────────────────────────────────────────
    public string PlatformNameEn { get; set; } = "Skanly";
    public string PlatformNameAr { get; set; } = "سكانلي";
}