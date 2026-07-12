// Skanly.Application/Features/Payments/DTOs/CheckoutViewModel.cs
namespace Skanly.Application.Features.Payments.DTOs;

/// <summary>
/// ViewModel assembled by the controller for the checkout page.
/// Contains all booking context needed for the payment form.
/// </summary>
public class CheckoutViewModel
{
    public int BookingId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public string? PropertyImageUrl { get; init; }
    public string OwnerFullName { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public DateOnly CheckInDate { get; init; }
    public DateOnly? CheckOutDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal CommissionRate { get; init; }
    public InitiatePaymentDto PaymentForm { get; init; } = new();
}