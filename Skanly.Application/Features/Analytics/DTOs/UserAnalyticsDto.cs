// Skanly.Application/Features/Analytics/DTOs/UserAnalyticsDto.cs
namespace Skanly.Application.Features.Analytics.DTOs;

public class UserAnalyticsDto
{
    // ── Totals ─────────────────────────────────────────────────────────────────
    public int TotalRegistered { get; init; }
    public int TotalStudents { get; init; }
    public int TotalOwners { get; init; }
    public int TotalAdmins { get; init; }
    public int VerifiedStudents { get; init; }
    public int VerifiedOwners { get; init; }
    public int ActiveUsers { get; init; }
    public int InactiveUsers { get; init; }

    // ── In date range ─────────────────────────────────────────────────────────
    public int NewRegistrations { get; init; }
    public int PendingVerifications { get; init; }
    public int ApprovedVerifications { get; init; }
    public int RejectedVerifications { get; init; }

    // ── Charts ────────────────────────────────────────────────────────────────
    public ChartDataDto RegistrationTrendChart { get; init; } = new();
    public PieChartDto UserRoleDistribution { get; init; } = new();
    public PieChartDto VerificationStatusChart { get; init; } = new();
    public ChartDataDto VerificationTrendChart { get; init; } = new();

    // ── Top universities ──────────────────────────────────────────────────────
    public IReadOnlyList<UniversityStatsRow> TopUniversities { get; init; }
        = new List<UniversityStatsRow>();

    // ── New user list (recent) ─────────────────────────────────────────────────
    public IReadOnlyList<RecentUserRow> RecentUsers { get; init; }
        = new List<RecentUserRow>();

    public DateRangeDto DateRange { get; init; } = DateRangeDto.Last30Days();
}

public class UniversityStatsRow
{
    public string UniversityNameEn { get; init; } = string.Empty;
    public int StudentCount { get; init; }
    public int PropertyCount { get; init; }
    public int BookingCount { get; init; }
}

public class RecentUserRow
{
    public string UserId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public DateTime RegisteredAt { get; init; }
}