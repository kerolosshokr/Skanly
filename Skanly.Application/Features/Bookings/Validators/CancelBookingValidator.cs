// Skanly.Application/Features/Bookings/Validators/CancelBookingValidator.cs
using FluentValidation;
using Skanly.Application.Features.Bookings.DTOs;

namespace Skanly.Application.Features.Bookings.Validators;

public class CancelBookingValidator : AbstractValidator<CancelBookingDto>
{
    public CancelBookingValidator()
    {
        RuleFor(x => x.BookingId)
            .GreaterThan(0)
            .WithMessage("Invalid booking reference.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Please provide a reason for cancellation.")
            .MinimumLength(10)
            .WithMessage("Cancellation reason must be at least 10 characters.")
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}