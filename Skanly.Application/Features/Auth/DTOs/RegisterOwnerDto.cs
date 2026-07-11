// Skanly.Application/Features/Auth/DTOs/RegisterOwnerDto.cs
namespace Skanly.Application.Features.Auth.DTOs;

public class RegisterOwnerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public bool AgreeToTerms { get; set; }
}