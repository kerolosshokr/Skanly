// Skanly.Application/Features/Students/DTOs/CompleteProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Students.DTOs;

/// <summary>
/// Used during the post-registration onboarding wizard.
/// Collects the minimum required info before the student can search.
/// </summary>
public class CompleteProfileDto
{
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Gender")]
    public byte Gender { get; set; }

    [Display(Name = "Date of Birth")]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "University")]
    public int? UniversityId { get; set; }
}