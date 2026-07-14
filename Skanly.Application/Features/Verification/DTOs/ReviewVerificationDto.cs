// Skanly.Application/Features/Verification/DTOs/ReviewVerificationDto.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Verification.DTOs;

public class ReviewVerificationDto
{
    [Required]
    public int VerificationId { get; set; }

    [Required]
    public VerificationStatus Decision { get; set; }

    // Editable extracted fields — Admin can correct OCR errors
    [Display(Name = "Full Name (as on ID)")]
    public string? VerifiedName { get; set; }

    [Display(Name = "National ID Number")]
    public string? VerifiedNationalId { get; set; }

    [Display(Name = "Date of Birth")]
    public DateOnly? VerifiedBirthDate { get; set; }

    [Display(Name = "Rejection Reason")]
    public string? RejectionReason { get; set; }
}