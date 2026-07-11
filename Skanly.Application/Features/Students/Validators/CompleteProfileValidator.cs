// Skanly.Application/Features/Students/Validators/CompleteProfileValidator.cs
using FluentValidation;
using Skanly.Application.Features.Students.DTOs;

namespace Skanly.Application.Features.Students.Validators;

public class CompleteProfileValidator : AbstractValidator<CompleteProfileDto>
{
    public CompleteProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^(\+20|0)(10|11|12|15)[0-9]{8}$")
            .WithMessage("Please enter a valid Egyptian phone number.");

        RuleFor(x => x.Gender)
            .InclusiveBetween((byte)1, (byte)2)
            .WithMessage("Please select your gender.");

        RuleFor(x => x.BirthDate)
            .LessThan(DateOnly.FromDateTime(DateTime.Today.AddYears(-16)))
            .WithMessage("You must be at least 16 years old.")
            .When(x => x.BirthDate.HasValue);
    }
}