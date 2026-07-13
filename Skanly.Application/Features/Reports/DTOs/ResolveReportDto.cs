// Skanly.Application/Features/Reports/DTOs/ResolveReportDto.cs
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Reports.DTOs;

public class ResolveReportDto
{
    [Required]
    public int ReportId { get; set; }

    [Required]
    public ReportStatus NewStatus { get; set; }

    [Required]
    [Display(Name = "Resolution Notes")]
    public string Resolution { get; set; } = string.Empty;

    [Display(Name = "Take Action Against User")]
    public bool DeactivateUser { get; set; }

    [Display(Name = "Remove Reported Property")]
    public bool RemoveProperty { get; set; }
}