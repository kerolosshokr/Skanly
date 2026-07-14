// Skanly.Application/Features/Verification/DTOs/SubmitVerificationDto.cs
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Verification.DTOs;

public class SubmitVerificationDto
{
    [Display(Name = "National ID — Front Side")]
    public IFormFile NationalIdFront { get; set; } = null!;

    [Display(Name = "National ID — Back Side")]
    public IFormFile? NationalIdBack { get; set; }
}