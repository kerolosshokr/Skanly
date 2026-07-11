// Skanly.Application/Features/Auth/DTOs/AuthResultDto.cs
namespace Skanly.Application.Features.Auth.DTOs;

public class AuthResultDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiry { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiry { get; set; }
    public string? ProfileImageUrl { get; set; }
}