// Skanly.Infrastructure/Identity/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace Skanly.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Refresh token support
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}