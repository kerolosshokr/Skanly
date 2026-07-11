// Skanly.Application/Features/Students/Validators/UploadIdentityValidator.cs
using FluentValidation;
using Skanly.Application.Features.Students.DTOs;

namespace Skanly.Application.Features.Students.Validators;

public class UploadIdentityValidator : AbstractValidator<UploadIdentityDto>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadIdentityValidator()
    {
        RuleFor(x => x.NationalIdFront)
            .NotNull().WithMessage("Front of National ID is required.")
            .Must(f => f.Length <= MaxFileSizeBytes)
            .WithMessage("File size must not exceed 5 MB.")
            .Must(f => AllowedExtensions.Contains(
                Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only JPG, PNG, and PDF files are accepted.");

        RuleFor(x => x.NationalIdBack)
            .Must(f => f == null || f.Length <= MaxFileSizeBytes)
            .WithMessage("Back file size must not exceed 5 MB.")
            .Must(f => f == null || AllowedExtensions.Contains(
                Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Only JPG, PNG, and PDF files are accepted.")
            .When(x => x.NationalIdBack != null);
    }
}