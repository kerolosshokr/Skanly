// Skanly.Infrastructure/ExternalServices/Payment/SimulatedMastercardGateway.cs
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.ExternalServices.Payment;

public class SimulatedMastercardGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.Mastercard;

    public Task<GatewayResult> ProcessAsync(
        GatewayRequest request,
        CancellationToken ct = default)
    {
        if (request.CardNumber?.EndsWith("1111") == true)
        {
            return Task.FromResult(GatewayResult.Failure(
                "CARD_BLOCKED",
                "This card has been blocked. Please contact your bank."));
        }

        var txRef = $"MC-{Guid.NewGuid():N}".ToUpper()[..18];

        var random = new Random();
        if (random.Next(100) < 8)
        {
            return Task.FromResult(GatewayResult.Failure(
                "TRANSACTION_LIMIT",
                "Transaction exceeds your daily limit."));
        }

        return Task.FromResult(GatewayResult.Success(txRef));
    }
}