namespace Skanly.Application.Common.DTOs;

public class IdentityUserDto
{
    public string Id { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }
}