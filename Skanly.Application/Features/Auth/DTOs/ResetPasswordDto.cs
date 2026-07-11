// Skanly.Application/Features/Auth/DTOs/ResetPasswordDto.cs
namespace Skanly.Application.Features.Auth.DTOs;

public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}