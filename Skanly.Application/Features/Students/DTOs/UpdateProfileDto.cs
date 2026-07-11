// Skanly.Application/Features/Students/DTOs/UpdateProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Students.DTOs;

public class UpdateProfileDto
{
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Date of Birth")]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "University")]
    public int? UniversityId { get; set; }
}