// Skanly.Application/Features/Verification/Validators/ReviewVerificationValidator.cs
using FluentValidation;
using Skanly.Application.Features.Verification.DTOs;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Verification.Validators;

public class ReviewVerificationValidator
    : AbstractValidator<ReviewVerificationDto>
{
    public ReviewVerificationValidator()
    {
        RuleFor(x => x.VerificationId)
            .GreaterThan(0)
            .WithMessage("Invalid verification ID.");

        RuleFor(x => x.Decision)
            .Must(d => d == VerificationStatus.Approved ||
                       d == VerificationStatus.Rejected)
            .WithMessage("Decision must be Approved or Rejected.");

        // If approving, verified fields must be present
        When(x => x.Decision == VerificationStatus.Approved, () =>
        {
            RuleFor(x => x.VerifiedName)
                .NotEmpty()
                .WithMessage("Please confirm the name from the ID.")
                .MaximumLength(150);

            RuleFor(x => x.VerifiedNationalId)
                .NotEmpty()
                .WithMessage("Please confirm the National ID number.")
                .Length(14)
                .WithMessage("Egyptian National ID must be exactly 14 digits.")
                .Matches(@"^\d{14}$")
                .WithMessage("National ID must contain only digits.");
        });

        // If rejecting, reason is mandatory
        When(x => x.Decision == VerificationStatus.Rejected, () =>
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .WithMessage("Please provide a reason for rejection.")
                .MinimumLength(10)
                .WithMessage("Rejection reason must be at least 10 characters.")
                .MaximumLength(500)
                .WithMessage("Rejection reason cannot exceed 500 characters.");
        });
    }
}