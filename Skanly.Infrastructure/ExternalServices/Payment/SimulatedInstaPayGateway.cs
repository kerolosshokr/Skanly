// Skanly.Infrastructure/ExternalServices/Payment/SimulatedInstaPayGateway.cs
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.ExternalServices.Payment;

public class SimulatedInstaPayGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.InstaPay;

    public async Task<GatewayResult> ProcessAsync(
        GatewayRequest request,
        CancellationToken ct = default)
    {
        await Task.Delay(600, ct);

        if (request.MobileNumber?.EndsWith("8888") == true)
        {
            return GatewayResult.Failure(
                "INSTAPAY_NOT_FOUND",
                "No InstaPay account linked to this number.");
        }

        var txRef = $"IP-{Guid.NewGuid():N}".ToUpper()[..16];

        return GatewayResult.Success(txRef);
    }
}