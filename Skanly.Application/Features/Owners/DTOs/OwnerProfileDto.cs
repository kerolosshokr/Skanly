// Skanly.Application/Features/Owners/DTOs/OwnerProfileDto.cs
namespace Skanly.Application.Features.Owners.DTOs;

public class OwnerProfileDto
{
    public string UserId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? BusinessName { get; init; }
    public string? ProfileImageUrl { get; init; }
    public bool IsIdentityVerified { get; init; }
    public string VerificationStatus { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    // Stats
    public int TotalProperties { get; init; }
    public int ActiveListings { get; init; }
    public int TotalBookings { get; init; }
    public int PendingRequests { get; init; }
    public decimal TotalEarnings { get; init; }
}