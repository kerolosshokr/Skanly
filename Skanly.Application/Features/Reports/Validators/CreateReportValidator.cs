// Skanly.Application/Features/Reports/Validators/CreateReportValidator.cs
using FluentValidation;
using Skanly.Application.Features.Reports.DTOs;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Reports.Validators;

public class CreateReportValidator : AbstractValidator<CreateReportDto>
{
    private static readonly string[] AllowedEvidenceTypes =
        { ".jpg", ".jpeg", ".png", ".pdf", ".webp" };
    private const long MaxEvidenceBytes = 10 * 1024 * 1024; // 10 MB

    public CreateReportValidator()
    {
        // Must target at least one entity
        RuleFor(x => x)
            .Must(x => x.ReportedPropertyId.HasValue ||
                       !string.IsNullOrWhiteSpace(x.ReportedUserId))
            .WithMessage("A report must target either a property or a user.");

        RuleFor(x => x.ReportType)
            .IsInEnum()
            .WithMessage("Please select a valid report type.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Please describe the issue.")
            .MinimumLength(20)
            .WithMessage("Description must be at least 20 characters. " +
                         "Please provide enough detail for our team to investigate.")
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.");

        // Property-specific type guard
        RuleFor(x => x.ReportedPropertyId)
            .NotNull()
            .WithMessage("Please specify which property this issue relates to.")
            .When(x => x.ReportType == ReportType.FakeListing ||
                       x.ReportType == ReportType.PropertyIssue);

        // User-specific type guard
        RuleFor(x => x.ReportedUserId)
            .NotEmpty()
            .WithMessage("Please specify which user this report is about.")
            .When(x => x.ReportType == ReportType.FraudulentOwner);

        // Evidence file validation (optional)
        When(x => x.Evidence != null, () =>
        {
            RuleFor(x => x.Evidence!.Length)
                .LessThanOrEqualTo(MaxEvidenceBytes)
                .WithMessage("Evidence file must not exceed 10 MB.");

            RuleFor(x => x.Evidence!.FileName)
                .Must(name => AllowedEvidenceTypes.Contains(
                    Path.GetExtension(name).ToLowerInvariant()))
                .WithMessage("Evidence must be a JPG, PNG, PDF, or WEBP file.");
        });
    }
}