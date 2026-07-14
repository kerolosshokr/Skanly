// Skanly.Application/Features/Verification/Validators/SubmitVerificationValidator.cs
using FluentValidation;
using Skanly.Application.Features.Verification.DTOs;

namespace Skanly.Application.Features.Verification.Validators;

public class SubmitVerificationValidator
    : AbstractValidator<SubmitVerificationDto>
{
    private static readonly string[] AllowedTypes =
        { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public SubmitVerificationValidator()
    {
        RuleFor(x => x.NationalIdFront)
            .NotNull()
            .WithMessage("Front side of National ID is required.")
            .Must(f => f.Length <= MaxBytes)
            .WithMessage("Front image must not exceed 5 MB.")
            .Must(f => AllowedTypes.Contains(
                Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only JPG, PNG, and WEBP images are accepted.");

        When(x => x.NationalIdBack is not null, () =>
        {
            RuleFor(x => x.NationalIdBack!)
                .Must(f => f.Length <= MaxBytes)
                .WithMessage("Back image must not exceed 5 MB.")
                .Must(f => AllowedTypes.Contains(
                    Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Only JPG, PNG, and WEBP images are accepted.");
        });
    }
}