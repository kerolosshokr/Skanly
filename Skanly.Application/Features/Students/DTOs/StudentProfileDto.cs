// Skanly.Application/Features/Students/DTOs/StudentProfileDto.cs
namespace Skanly.Application.Features.Students.DTOs;

public class StudentProfileDto
{
    public string UserId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string GenderDisplay { get; init; } = string.Empty;
    public DateOnly? BirthDate { get; init; }
    public string? NationalId { get; init; }
    public string? ProfileImageUrl { get; init; }
    public bool IsIdentityVerified { get; init; }
    public string VerificationStatus { get; init; } = string.Empty;
    public int? UniversityId { get; init; }
    public string? UniversityNameEn { get; init; }
    public string? UniversityNameAr { get; init; }
    public bool IsProfileComplete { get; init; }
    public DateTime CreatedAt { get; init; }

    // Stats
    public int TotalBookings { get; init; }
    public int ActiveBookings { get; init; }
    public int TotalFavorites { get; init; }
    public int TotalReviews { get; init; }
}