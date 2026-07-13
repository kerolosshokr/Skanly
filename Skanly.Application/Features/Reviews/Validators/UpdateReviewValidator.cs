// Skanly.Application/Features/Reviews/Validators/UpdateReviewValidator.cs
using FluentValidation;
using Skanly.Application.Features.Reviews.DTOs;

namespace Skanly.Application.Features.Reviews.Validators;

public class UpdateReviewValidator : AbstractValidator<UpdateReviewDto>
{
    public UpdateReviewValidator()
    {
        RuleFor(x => x.ReviewId)
            .GreaterThan(0)
            .WithMessage("Invalid review ID.");

        RuleFor(x => x.CleanlinessRating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Cleanliness rating must be between 1 and 5.");

        RuleFor(x => x.SafetyRating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Safety rating must be between 1 and 5.");

        RuleFor(x => x.InternetRating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Internet quality rating must be between 1 and 5.");

        RuleFor(x => x.LocationRating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Location rating must be between 1 and 5.");

        RuleFor(x => x.QuietnessRating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Quietness rating must be between 1 and 5.");

        RuleFor(x => x.OverallRating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Overall experience rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}