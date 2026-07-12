// Skanly.Application/Features/Payments/Validators/InitiatePaymentValidator.cs
using FluentValidation;
using Skanly.Application.Features.Payments.DTOs;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Payments.Validators;

public class InitiatePaymentValidator : AbstractValidator<InitiatePaymentDto>
{
    public InitiatePaymentValidator()
    {
        RuleFor(x => x.BookingId)
            .GreaterThan(0).WithMessage("Invalid booking.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Please select a valid payment method.");

        // Card validation rules apply only for card methods
        When(x => x.PaymentMethod == PaymentMethod.Visa ||
                  x.PaymentMethod == PaymentMethod.Mastercard, () =>
                  {
                      RuleFor(x => x.CardNumber)
                          .NotEmpty().WithMessage("Card number is required.")
                          .Matches(@"^\d{16}$")
                          .WithMessage("Card number must be exactly 16 digits.");

                      RuleFor(x => x.CardHolderName)
                          .NotEmpty().WithMessage("Cardholder name is required.")
                          .MaximumLength(100);

                      RuleFor(x => x.CardExpiry)
                          .NotEmpty().WithMessage("Expiry date is required.")
                          .Matches(@"^(0[1-9]|1[0-2])\/\d{2}$")
                          .WithMessage("Expiry must be in MM/YY format.")
                          .Must(BeValidExpiry)
                          .WithMessage("Card has expired.");

                      RuleFor(x => x.CardCvv)
                          .NotEmpty().WithMessage("CVV is required.")
                          .Matches(@"^\d{3,4}$")
                          .WithMessage("CVV must be 3 or 4 digits.");
                  });

        // Mobile number required for wallet methods
        When(x => x.PaymentMethod == PaymentMethod.VodafoneCash ||
                  x.PaymentMethod == PaymentMethod.InstaPay, () =>
                  {
                      RuleFor(x => x.MobileNumber)
                          .NotEmpty().WithMessage("Mobile number is required.")
                          .Matches(@"^(\+20|0)(10|11|12|15)[0-9]{8}$")
                          .WithMessage("Please enter a valid Egyptian mobile number.");
                  });

        // Fawry: no extra input needed (reference code generated on our side)
    }

    private static bool BeValidExpiry(string? expiry)
    {
        if (string.IsNullOrEmpty(expiry)) return false;
        var parts = expiry.Split('/');
        if (parts.Length != 2) return false;

        if (!int.TryParse(parts[0], out var month) ||
            !int.TryParse(parts[1], out var year)) return false;

        var expiryDate = new DateTime(2000 + year, month, 1)
            .AddMonths(1).AddDays(-1);

        return expiryDate >= DateTime.Today;
    }
}