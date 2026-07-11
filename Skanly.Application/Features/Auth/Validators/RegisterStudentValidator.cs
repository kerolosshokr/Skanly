// Skanly.Application/Features/Auth/Validators/RegisterStudentValidator.cs
using FluentValidation;
using Skanly.Application.Features.Auth.DTOs;

namespace Skanly.Application.Features.Auth.Validators;

public class RegisterStudentValidator : AbstractValidator<RegisterStudentDto>
{
    public RegisterStudentValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^(\+20|0)(10|11|12|15)[0-9]{8}$")
            .WithMessage("Please enter a valid Egyptian phone number.");

        RuleFor(x => x.Gender)
            .InclusiveBetween((byte)1, (byte)2)
            .WithMessage("Please select a valid gender.");

        RuleFor(x => x.AgreeToTerms)
            .Equal(true).WithMessage("You must agree to the terms and conditions.");
    }
}