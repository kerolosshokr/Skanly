// Skanly.Application/Features/Properties/Validators/CreatePropertyValidator.cs
using FluentValidation;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Properties.Validators;

public class CreatePropertyValidator : AbstractValidator<CreatePropertyDto>
{
    private static readonly string[] AllowedImageTypes =
        { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly string[] AllowedVideoTypes =
        { ".mp4", ".mov", ".avi", ".webm" };

    private const long MaxImageSize = 5 * 1024 * 1024;   // 5 MB
    private const long MaxVideoSize = 50 * 1024 * 1024;  // 50 MB

    public CreatePropertyValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Property title is required.")
            .MinimumLength(10).WithMessage("Title must be at least 10 characters.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Please select a valid property type.");

        RuleFor(x => x.Rooms)
            .GreaterThan(0).WithMessage("Number of rooms must be at least 1.")
            .LessThanOrEqualTo(50).WithMessage("Number of rooms cannot exceed 50.");

        RuleFor(x => x.Beds)
            .GreaterThan(0).WithMessage("Number of beds must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("Number of beds cannot exceed 100.");

        RuleFor(x => x.PricePerMonth)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Price seems unrealistically high. Please verify.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300).WithMessage("Address cannot exceed 300 characters.");

        RuleFor(x => x.Latitude)
            .NotEmpty().WithMessage("Location is required.")
            .InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .NotEmpty().WithMessage("Location is required.")
            .InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.AreaId)
            .GreaterThan(0).WithMessage("Please select an area.");

        // Image validation
        RuleFor(x => x.Images)
            .NotEmpty().WithMessage("At least one photo is required.")
            .Must(imgs => imgs.Count <= 15)
            .WithMessage("Maximum 15 photos allowed.");

        RuleForEach(x => x.Images)
            .Must(f => f.Length <= MaxImageSize)
            .WithMessage("Each photo must not exceed 5 MB.")
            .Must(f => AllowedImageTypes.Contains(
                Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only JPG, PNG, and WEBP images are accepted.");

        // Video validation (optional)
        RuleFor(x => x.Videos)
            .Must(v => v.Count <= 3)
            .WithMessage("Maximum 3 videos allowed.")
            .When(x => x.Videos.Any());

        RuleForEach(x => x.Videos)
            .Must(f => f.Length <= MaxVideoSize)
            .WithMessage("Each video must not exceed 50 MB.")
            .Must(f => AllowedVideoTypes.Contains(
                Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only MP4, MOV, AVI, and WEBM videos are accepted.")
            .When(x => x.Videos.Any());
    }
}