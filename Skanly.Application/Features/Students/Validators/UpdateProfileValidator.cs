// Skanly.Application/Features/Students/Validators/UpdateProfileValidator.cs
using FluentValidation;
using Skanly.Application.Features.Students.DTOs;

namespace Skanly.Application.Features.Students.Validators;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
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

        RuleFor(x => x.BirthDate)
            .LessThan(DateOnly.FromDateTime(DateTime.Today.AddYears(-16)))
            .WithMessage("You must be at least 16 years old.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Today.AddYears(-100)))
            .WithMessage("Please enter a valid date of birth.")
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.UniversityId)
            .GreaterThan(0).WithMessage("Please select a valid university.")
            .When(x => x.UniversityId.HasValue);
    }
}