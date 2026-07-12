// Skanly.Application/Features/Properties/Validators/UpdatePropertyValidator.cs
using FluentValidation;
using Skanly.Application.Features.Properties.DTOs;

namespace Skanly.Application.Features.Properties.Validators;

public class UpdatePropertyValidator : AbstractValidator<UpdatePropertyDto>
{
    private static readonly string[] AllowedImageTypes =
        { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedVideoTypes =
        { ".mp4", ".mov", ".avi", ".webm" };
    private const long MaxImageSize = 5 * 1024 * 1024;
    private const long MaxVideoSize = 50 * 1024 * 1024;

    public UpdatePropertyValidator()
    {
        RuleFor(x => x.PropertyId)
            .GreaterThan(0).WithMessage("Invalid property ID.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(10)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Please select a valid property type.");

        RuleFor(x => x.Rooms)
            .GreaterThan(0).LessThanOrEqualTo(50);

        RuleFor(x => x.Beds)
            .GreaterThan(0).LessThanOrEqualTo(100);

        RuleFor(x => x.PricePerMonth)
            .GreaterThan(0).LessThanOrEqualTo(1_000_000);

        RuleFor(x => x.Address)
            .NotEmpty().MaximumLength(300);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m);

        RuleFor(x => x.AreaId)
            .GreaterThan(0).WithMessage("Please select an area.");

        RuleForEach(x => x.NewImages)
            .Must(f => f.Length <= MaxImageSize)
            .WithMessage("Each photo must not exceed 5 MB.")
            .Must(f => AllowedImageTypes.Contains(
                Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only JPG, PNG, WEBP images accepted.")
            .When(x => x.NewImages.Any());

        RuleForEach(x => x.NewVideos)
            .Must(f => f.Length <= MaxVideoSize)
            .WithMessage("Each video must not exceed 50 MB.")
            .Must(f => AllowedVideoTypes.Contains(
                Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only MP4, MOV, AVI, WEBM accepted.")
            .When(x => x.NewVideos.Any());
    }
}