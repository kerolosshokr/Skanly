// Skanly.Application/Features/Students/DTOs/StudentDashboardDto.cs
using Skanly.Application.Features.Bookings.DTOs;
using Skanly.Application.Features.Notifications.DTOs;
using Skanly.Application.Features.Properties.DTOs;

namespace Skanly.Application.Features.Students.DTOs;

public class StudentDashboardDto
{
    // Profile snapshot
    public string FullName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public bool IsIdentityVerified { get; init; }
    public bool IsProfileComplete { get; init; }
    public string? UniversityNameEn { get; init; }
    public string VerificationStatus { get; init; } = string.Empty;

    // Stats cards
    public int TotalBookings { get; init; }
    public int ActiveBookings { get; init; }
    public int PendingBookings { get; init; }
    public int TotalFavorites { get; init; }
    public int UnreadNotifications { get; init; }

    //Quick lists
        public IReadOnlyList<BookingSummaryDto> RecentBookings { get; init; }
            = new List<BookingSummaryDto>();

    public IReadOnlyList<PropertyCardDto> RecentFavorites { get; init; }
        = new List<PropertyCardDto>();

    public IReadOnlyList<NotificationDto> RecentNotifications { get; init; }
        = new List<NotificationDto>();
}
