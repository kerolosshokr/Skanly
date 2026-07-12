// Skanly.Infrastructure/ExternalServices/Payment/SimulatedVisaGateway.cs
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.ExternalServices.Payment;

/// <summary>
/// Phase 1 simulation. Simulates 90% success rate.
/// Phase 2: replace body with Stripe / Paymob Visa SDK call.
/// </summary>
public class SimulatedVisaGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.Visa;

    public Task<GatewayResult> ProcessAsync(
        GatewayRequest request,
        CancellationToken ct = default)
    {
        // Simulate card validation: cards ending in 0000 always fail
        if (request.CardNumber?.EndsWith("0000") == true)
        {
            return Task.FromResult(GatewayResult.Failure(
                "CARD_DECLINED",
                "Your card was declined. Please check your card details."));
        }

        var txRef = $"VISA-{Guid.NewGuid():N}".ToUpper()[..20];

        // 90% simulated success rate
        var random = new Random();
        if (random.Next(100) < 10)
        {
            return Task.FromResult(GatewayResult.Failure(
                "INSUFFICIENT_FUNDS",
                "Insufficient funds. Please use a different card."));
        }

        return Task.FromResult(GatewayResult.Success(txRef));
    }
}