// Skanly.Application/Features/Auth/DTOs/RefreshTokenDto.cs
namespace Skanly.Application.Features.Auth.DTOs;

public class RefreshTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}