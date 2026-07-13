// Skanly.Application/Features/Reports/DTOs/CreateReportDto.cs
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Reports.DTOs;

public class CreateReportDto
{
    // Target — at least one must be provided
    [Display(Name = "Property (if applicable)")]
    public int? ReportedPropertyId { get; set; }

    [Display(Name = "User (if applicable)")]
    public string? ReportedUserId { get; set; }

    [Required]
    [Display(Name = "Report Type")]
    public ReportType ReportType { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Evidence (screenshot/photo)")]
    public IFormFile? Evidence { get; set; }
}