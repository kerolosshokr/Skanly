// Skanly.Infrastructure/ExternalServices/Payment/PaymentGatewayFactory.cs
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Infrastructure.ExternalServices.Payment;

/// <summary>
/// Resolves the correct IPaymentGateway for a given PaymentMethod.
/// New gateways (Paymob, Stripe) are added here in Phase 2 —
/// no other code changes required.
/// </summary>
public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways;
    }

    public IPaymentGateway GetGateway(PaymentMethod method)
    {
        var gateway = _gateways.FirstOrDefault(g => g.Method == method);

        if (gateway is null)
            throw new NotSupportedException(
                $"No payment gateway registered for method: {method}. " +
                $"Register a new IPaymentGateway implementation in DI.");

        return gateway;
    }

    public IReadOnlyList<PaymentMethod> GetSupportedMethods()
        => _gateways.Select(g => g.Method).ToList();
}