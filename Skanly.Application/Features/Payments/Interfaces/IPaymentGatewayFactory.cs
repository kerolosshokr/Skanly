using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Payments.Interfaces;

public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(PaymentMethod method);

    IReadOnlyList<PaymentMethod> GetSupportedMethods();
}