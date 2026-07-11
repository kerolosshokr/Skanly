// Skanly.Application/Features/Universities/Validators/UpdateUniversityValidator.cs
using FluentValidation;
using Skanly.Application.Features.Universities.DTOs;

namespace Skanly.Application.Features.Universities.Validators;

public class UpdateUniversityValidator : AbstractValidator<UpdateUniversityDto>
{
    public UpdateUniversityValidator()
    {
        RuleFor(x => x.UniversityId)
            .GreaterThan(0).WithMessage("Invalid university ID.");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required.")
            .MaximumLength(150).WithMessage("Arabic name cannot exceed 150 characters.")
            .Matches(@"[\u0600-\u06FF\s]+")
            .WithMessage("Arabic name must contain Arabic characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required.")
            .MaximumLength(150).WithMessage("English name cannot exceed 150 characters.")
            .Matches(@"^[a-zA-Z\s\-']+$")
            .WithMessage("English name must contain only English letters.");

        RuleFor(x => x.Address)
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}