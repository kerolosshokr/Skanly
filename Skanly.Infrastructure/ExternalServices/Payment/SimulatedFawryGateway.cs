// Skanly.Infrastructure/ExternalServices/Payment/SimulatedFawryGateway.cs
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.ExternalServices.Payment;

/// <summary>
/// Fawry works differently — generates a reference code that the
/// customer takes to a Fawry point to pay. In simulation, we mark
/// it as Success immediately. In Phase 2, Fawry sends a webhook
/// when the cash is collected.
/// </summary>
public class SimulatedFawryGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.Fawry;

    public Task<GatewayResult> ProcessAsync(
        GatewayRequest request,
        CancellationToken ct = default)
    {
        // Generate a Fawry-style 10-digit reference
        var fawryRef = $"FWR{new Random().Next(1_000_000, 9_999_999)}";

        // In simulation: always success
        // In Phase 2: return pending status + reference,
        // webhook handler will call ConfirmFawryPaymentAsync
        return Task.FromResult(GatewayResult.Success(fawryRef));
    }
}