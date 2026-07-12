// Skanly.Application/Features/Owners/Validators/HandleBookingRequestValidator.cs
using FluentValidation;
using Skanly.Application.Features.Owners.DTOs;

namespace Skanly.Application.Features.Owners.Validators;

public class HandleBookingRequestValidator : AbstractValidator<HandleBookingRequestDto>
{
    public HandleBookingRequestValidator()
    {
        RuleFor(x => x.BookingId)
            .GreaterThan(0).WithMessage("Invalid booking ID.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Please provide a reason for rejection.")
            .MaximumLength(500).WithMessage("Rejection reason cannot exceed 500 characters.")
            .When(x => !x.Accept);
    }
}