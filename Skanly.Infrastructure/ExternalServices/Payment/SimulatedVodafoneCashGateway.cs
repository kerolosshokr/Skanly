// Skanly.Infrastructure/ExternalServices/Payment/SimulatedVodafoneCashGateway.cs
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.ExternalServices.Payment;

public class SimulatedVodafoneCashGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.VodafoneCash;

    public async Task<GatewayResult> ProcessAsync(
        GatewayRequest request,
        CancellationToken ct = default)
    {
        // Simulate network latency for mobile payment
        await Task.Delay(500, ct);

        // Numbers ending in 9999 simulate unregistered wallet
        if (request.MobileNumber?.EndsWith("9999") == true)
        {
            return GatewayResult.Failure(
                "WALLET_NOT_REGISTERED",
                "This number is not registered with Vodafone Cash.");
        }

        var txRef = $"VFC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N[..8]}"
            .ToUpper();

        var random = new Random();
        if (random.Next(100) < 5)
        {
            return GatewayResult.Failure(
                "WALLET_INSUFFICIENT_BALANCE",
                "Insufficient Vodafone Cash balance.");
        }

        return GatewayResult.Success(txRef);
    }
}