// Skanly.Application/Features/Payments/DTOs/PaymentResultDto.cs
namespace Skanly.Application.Features.Payments.DTOs;

/// <summary>
/// Returned after a payment attempt — success or failure.
/// </summary>
public class PaymentResultDto
{
    public bool IsSuccess { get; init; }
    public int PaymentId { get; init; }
    public int BookingId { get; init; }
    public string? TransactionReference { get; init; }
    public decimal AmountPaid { get; init; }
    public string? FailureReason { get; init; }
    public string? RedirectUrl { get; init; }

    public static PaymentResultDto Success(
        int paymentId,
        int bookingId,
        string transactionRef,
        decimal amount) => new()
        {
            IsSuccess = true,
            PaymentId = paymentId,
            BookingId = bookingId,
            TransactionReference = transactionRef,
            AmountPaid = amount
        };

    public static PaymentResultDto Failure(
        int bookingId,
        string reason) => new()
        {
            IsSuccess = false,
            BookingId = bookingId,
            FailureReason = reason
        };
}