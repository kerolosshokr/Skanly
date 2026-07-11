// Skanly.Application/Features/Auth/Validators/LoginValidator.cs
using FluentValidation;
using Skanly.Application.Features.Auth.DTOs;

namespace Skanly.Application.Features.Auth.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}