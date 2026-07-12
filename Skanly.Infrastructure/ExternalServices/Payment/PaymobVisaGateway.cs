// PHASE 2 EXAMPLE — Paymob Visa Gateway
// Skanly.Infrastructure/ExternalServices/Payment/PaymobVisaGateway.cs

// REPLACE SimulatedVisaGateway with this class in DI.
// ZERO changes needed in PaymentService, controllers, or views.

using Microsoft.Extensions.Configuration;
using Skanly.Application.Features.Payments.Interfaces;
using Skanly.Domain.Enums;

public class PaymobVisaGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.Visa;

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public PaymobVisaGateway(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Paymob:ApiKey"]!;
    }

    public async Task<GatewayResult> ProcessAsync(
        GatewayRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Step 1: Authentication
            // Step 2: Order registration
            // Step 3: Payment key request
            // Step 4: Charge the card

            // On success:
            return GatewayResult.Success("PAYMOB-TXN-12345");

            // On failure:
            // return GatewayResult.Failure("DECLINED", "Card declined by issuer.");
        }
        catch (Exception ex)
        {
            return GatewayResult.Failure("GATEWAY_ERROR", ex.Message);
        }
    }
}