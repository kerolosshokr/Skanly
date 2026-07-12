// Skanly.Application/Features/Owners/Validators/UpdateOwnerProfileValidator.cs
using FluentValidation;
using Skanly.Application.Features.Owners.DTOs;

namespace Skanly.Application.Features.Owners.Validators;

public class UpdateOwnerProfileValidator : AbstractValidator<UpdateOwnerProfileDto>
{
    public UpdateOwnerProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.")
            .Matches(@"^[\p{L}\s\-']+$")
            .WithMessage("First name contains invalid characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.")
            .Matches(@"^[\p{L}\s\-']+$")
            .WithMessage("Last name contains invalid characters.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(\+20|0)(10|11|12|15)[0-9]{8}$")
            .WithMessage("Please enter a valid Egyptian phone number.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.BusinessName)
            .MaximumLength(150).WithMessage("Business name cannot exceed 150 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BusinessName));

        RuleFor(x => x.BankAccountInfo)
            .MaximumLength(300).WithMessage("Bank account info cannot exceed 300 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BankAccountInfo));
    }
}