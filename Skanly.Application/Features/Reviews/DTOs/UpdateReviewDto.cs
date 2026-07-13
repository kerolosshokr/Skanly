// Skanly.Application/Features/Reviews/DTOs/UpdateReviewDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Reviews.DTOs;

public class UpdateReviewDto
{
    [Required]
    public int ReviewId { get; set; }

    [Display(Name = "Cleanliness")]
    public byte CleanlinessRating { get; set; }

    [Display(Name = "Safety")]
    public byte SafetyRating { get; set; }

    [Display(Name = "Internet Quality")]
    public byte InternetRating { get; set; }

    [Display(Name = "Location")]
    public byte LocationRating { get; set; }

    [Display(Name = "Quietness")]
    public byte QuietnessRating { get; set; }

    [Display(Name = "Overall Experience")]
    public byte OverallRating { get; set; }

    [Display(Name = "Your Review")]
    public string? Comment { get; set; }
}