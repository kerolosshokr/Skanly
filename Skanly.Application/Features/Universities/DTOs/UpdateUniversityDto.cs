// Skanly.Application/Features/Universities/DTOs/UpdateUniversityDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Universities.DTOs;

public class UpdateUniversityDto
{
    public int UniversityId { get; set; }

    [Display(Name = "Arabic Name")]
    public string NameAr { get; set; } = string.Empty;

    [Display(Name = "English Name")]
    public string NameEn { get; set; } = string.Empty;

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "Latitude")]
    public decimal Latitude { get; set; }

    [Display(Name = "Longitude")]
    public decimal Longitude { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }
}