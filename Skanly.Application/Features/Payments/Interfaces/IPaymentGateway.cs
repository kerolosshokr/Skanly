// Skanly.Application/Features/Payments/Interfaces/IPaymentGateway.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Payments.Interfaces;

/// <summary>
/// Strategy interface for payment gateways.
/// Phase 1: simulation only.
/// Phase 2: implement with Paymob / Stripe SDK calls.
/// </summary>
public interface IPaymentGateway
{
    PaymentMethod Method { get; }

    /// <summary>
    /// Processes a payment and returns a gateway result.
    /// Implementations must NEVER throw — all failures are
    /// captured in GatewayResult.
    /// </summary>
    Task<GatewayResult> ProcessAsync(
        GatewayRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Request object passed to the gateway — gateway-agnostic.
/// </summary>
public class GatewayRequest
{
    public int BookingId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EGP";
    public string? CardNumber { get; init; }
    public string? CardHolderName { get; init; }
    public string? CardExpiry { get; init; }
    public string? CardCvv { get; init; }
    public string? MobileNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Gateway response — normalized across all providers.
/// </summary>
public class GatewayResult
{
    public bool IsSuccess { get; init; }
    public string TransactionReference { get; init; } = string.Empty;
    public string? FailureCode { get; init; }
    public string? FailureMessage { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;

    public static GatewayResult Success(string transactionRef) => new()
    {
        IsSuccess = true,
        TransactionReference = transactionRef
    };

    public static GatewayResult Failure(string code, string message) => new()
    {
        IsSuccess = false,
        FailureCode = code,
        FailureMessage = message
    };
}