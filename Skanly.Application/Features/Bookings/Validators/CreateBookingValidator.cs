// Skanly.Application/Features/Bookings/Validators/CreateBookingValidator.cs
using FluentValidation;
using Skanly.Application.Features.Bookings.DTOs;

namespace Skanly.Application.Features.Bookings.Validators;

public class CreateBookingValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.PropertyId)
            .GreaterThan(0)
            .WithMessage("Please select a valid property.");

        RuleFor(x => x.CheckInDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Check-in date cannot be in the past.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today.AddYears(1)))
            .WithMessage("Check-in date cannot be more than one year in advance.");

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Check-out date must be after check-in date.")
            .When(x => x.CheckOutDate.HasValue);

        RuleFor(x => x.SpecialRequests)
            .MaximumLength(500)
            .WithMessage("Special requests cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.SpecialRequests));
    }
}