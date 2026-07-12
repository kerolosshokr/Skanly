// Skanly.Application/Features/Bookings/DTOs/BookingDetailDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Bookings.DTOs;

/// <summary>
/// Full booking detail — used on the detail page.
/// Includes payments, contract, and review info.
/// </summary>
public class BookingDetailDto : BookingDto
{
    public IReadOnlyList<PaymentInfoDto> Payments { get; init; }
        = new List<PaymentInfoDto>();

    public ContractInfoDto? Contract { get; init; }
    public ReviewInfoDto? Review { get; init; }
    public string? RejectionReason { get; init; }
}

public class PaymentInfoDto
{
    public int PaymentId { get; init; }
    public string MethodDisplay { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? TransactionReference { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
}

public class ContractInfoDto
{
    public int ContractId { get; init; }
    public string ContractNumber { get; init; } = string.Empty;
    public string PdfUrl { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
}

public class ReviewInfoDto
{
    public int ReviewId { get; init; }
    public byte OverallRating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}