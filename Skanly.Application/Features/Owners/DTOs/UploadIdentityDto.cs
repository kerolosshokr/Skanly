using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Owners.DTOs;

public class UploadIdentityDto
{
    [Display(Name = "National ID Front")]
    public IFormFile NationalIdFront { get; set; } = null!;

    [Display(Name = "National ID Back")]
    public IFormFile? NationalIdBack { get; set; }
}
