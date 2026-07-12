// Skanly.Application/Features/Payments/DTOs/PaymentHistoryDto.cs
namespace Skanly.Application.Features.Payments.DTOs;

/// <summary>
/// Used for Admin payment management list view.
/// </summary>
public class PaymentHistoryDto
{
    public int PaymentId { get; init; }
    public int BookingId { get; init; }
    public string StudentFullName { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;
    public string PropertyTitle { get; init; } = string.Empty;
    public string PaymentMethodDisplay { get; init; } = string.Empty;
    public string PaymentMethodIcon { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal OwnerPayout { get; init; }
    public string? TransactionReference { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
}