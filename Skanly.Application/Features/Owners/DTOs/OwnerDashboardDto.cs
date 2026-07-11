using Skanly.Application.Features.Notifications.DTOs;

namespace Skanly.Application.Features.Owners.DTOs;

public class OwnerDashboardDto
{
    public string FullName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public bool IsIdentityVerified { get; init; }
    public string VerificationStatus { get; init; } = string.Empty;

    public int TotalProperties { get; init; }
    public int ActiveListings { get; init; }
    public int PendingRequests { get; init; }
    public int TotalBookings { get; init; }
    public decimal TotalEarnings { get; init; }
    public decimal MonthlyEarnings { get; init; }
    public int UnreadNotifications { get; init; }

    public IReadOnlyList<BookingRequestDto> PendingBookingRequests { get; init; }
        = new List<BookingRequestDto>();

    public IReadOnlyList<OwnerPropertySummaryDto> TopProperties { get; init; }
        = new List<OwnerPropertySummaryDto>();

    public IReadOnlyList<NotificationDto> RecentNotifications { get; init; }
        = new List<NotificationDto>();

    public IReadOnlyList<MonthlyEarningsPoint> EarningsChart { get; init; }
        = new List<MonthlyEarningsPoint>();
}

public class MonthlyEarningsPoint
{
    public string MonthLabel { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
