// Skanly.Application/Features/Owners/DTOs/UpdateOwnerProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Owners.DTOs;

public class UpdateOwnerProfileDto
{
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Business / Brand Name")]
    public string? BusinessName { get; set; }

    [Display(Name = "Bank Account Info")]
    public string? BankAccountInfo { get; set; }
}