// Skanly.Application/Features/Reports/Validators/ResolveReportValidator.cs
using FluentValidation;
using Skanly.Application.Features.Reports.DTOs;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Features.Reports.Validators;

public class ResolveReportValidator : AbstractValidator<ResolveReportDto>
{
    public ResolveReportValidator()
    {
        RuleFor(x => x.ReportId)
            .GreaterThan(0)
            .WithMessage("Invalid report ID.");

        RuleFor(x => x.NewStatus)
            .Must(s => s == ReportStatus.UnderInvestigation ||
                       s == ReportStatus.Resolved ||
                       s == ReportStatus.Dismissed)
            .WithMessage("Invalid status transition. " +
                         "Reports can only move to Under Investigation, " +
                         "Resolved, or Dismissed.");

        RuleFor(x => x.Resolution)
            .NotEmpty().WithMessage("Resolution notes are required.")
            .MinimumLength(10).WithMessage("Please provide more detail in your resolution notes.")
            .MaximumLength(1000).WithMessage("Resolution notes cannot exceed 1000 characters.");
    }
}