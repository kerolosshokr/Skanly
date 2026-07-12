// Skanly.Application/Features/Payments/DTOs/PaymentDto.cs
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Features.Payments.DTOs;

/// <summary>
/// Represents a single payment record returned to the presentation layer.
/// </summary>
public class PaymentDto
{
    public int PaymentId { get; init; }
    public int BookingId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public string? PropertyImageUrl { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public string PaymentMethodDisplay => PaymentMethod.ToString();
    public string PaymentMethodIcon => PaymentMethod switch
    {
        PaymentMethod.Visa => "fa-cc-visa",
        PaymentMethod.Mastercard => "fa-cc-mastercard",
        PaymentMethod.VodafoneCash => "fa-mobile-alt",
        PaymentMethod.InstaPay => "fa-bolt",
        PaymentMethod.Fawry => "fa-store",
        _ => "fa-credit-card"
    };
    public decimal Amount { get; init; }
    public string? TransactionReference { get; init; }
    public PaymentStatus Status { get; init; }
    public string StatusDisplay => Status.ToString();
    public string StatusBadgeClass => Status switch
    {
        PaymentStatus.Success => "bg-success",
        PaymentStatus.Failed => "bg-danger",
        _ => "bg-warning text-dark"
    };
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
}