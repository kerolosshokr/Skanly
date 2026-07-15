// Skanly.Application/Features/Analytics/Services/AnalyticsService.cs
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.DTOs;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Analytics.DTOs;
using Skanly.Application.Features.Analytics.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Features.Analytics.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly IIdentityService _identityService;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
    private const string CacheKeyPrefix = "analytics:";

    // Brand-consistent chart colors
    private static readonly string[] ChartColors =
    {
        "#6C63FF", "#10b981", "#f59e0b", "#ef4444",
        "#3b82f6", "#8b5cf6", "#06b6d4", "#f97316"
    };

    public AnalyticsService(
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<AnalyticsService> logger,
         IIdentityService identityService)
    {
        _uow = uow;
        _cache = cache;
        _logger = logger;
        _identityService = identityService;
    }

    // ── GetSummaryAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult<AnalyticsSummaryDto>> GetSummaryAsync(
        DateRangeDto range,
        CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}summary:{range.From:yyyyMMdd}:{range.To:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out AnalyticsSummaryDto? cached))
            return ServiceResult<AnalyticsSummaryDto>.Success(cached!);

        // Load raw data
        var allUsers = await _identityService.GetAllUsersAsync(ct);
        var allStudents = await _uow.Repository<Student>()
            .GetAllAsync(ct);
        var allOwners = await _uow.Repository<Owner>()
            .GetAllAsync(ct);
        var allProperties = await _uow.Repository<Property>()
            .GetAllAsync(
            ct: ct,
              includes:
              [
                  p => p.Area,
            p => p.Owner,
            p => p.Images,
            p => p.PropertyAmenities
                  ]
            );
        var allBookings = await _uow.Repository<Booking>()
            .GetAllAsync(ct);
        var allPayments = await _uow.Repository<Payment>()
            .GetAllAsync(ct);
        var allReviews = await _uow.Repository<Review>()
            .GetAllAsync(ct);
        var allReports = await _uow.Repository<Report>()
            .GetAllAsync(ct);

        // Filter to range
        var rangeUsers = allUsers.Where(u => u.CreatedAt >= range.From &&
                                                u.CreatedAt <= range.To).ToList();
        var rangeBookings = allBookings.Where(b => b.CreatedAt >= range.From &&
                                                    b.CreatedAt <= range.To).ToList();
        var rangePayments = allPayments.Where(p => p.PaidAt >= range.From &&
                                                    p.PaidAt <= range.To &&
                                                    p.Status == PaymentStatus.Success)
                                       .ToList();

        // Previous period for growth calculation
        var prevFrom = range.From.AddDays(-range.TotalDays);
        var prevTo = range.From.AddTicks(-1);
        var prevUsers = allUsers.Count(u => u.CreatedAt >= prevFrom &&
                                               u.CreatedAt <= prevTo);
        var prevBookings = allBookings.Count(b => b.CreatedAt >= prevFrom &&
                                                   b.CreatedAt <= prevTo);
        var prevRevenue = allPayments.Where(p => p.PaidAt >= prevFrom &&
                                                   p.PaidAt <= prevTo &&
                                                   p.Status == PaymentStatus.Success)
                                      .Sum(p => p.Amount);

        var totalRevenue = rangePayments.Sum(p => p.Amount);
        var confirmedBooks = allBookings
            .Where(b => b.Status == BookingStatus.Confirmed).ToList();
        var totalCommission = confirmedBooks.Sum(b => b.CommissionAmount ?? 0);

        // Daily activity chart (unique dates in range)
        var dailyChart = BuildDailyActivityChart(
            rangeUsers, rangeBookings, rangePayments, range);

        // Booking status pie chart
        var statusChart = new PieChartDto
        {
            Labels = new[]
            {
                "Confirmed", "Pending", "Accepted",
                "Cancelled", "Rejected", "Payment Pending"
            },
            Data = new double[]
            {
                allBookings.Count(b => b.Status == BookingStatus.Confirmed),
                allBookings.Count(b => b.Status == BookingStatus.Pending),
                allBookings.Count(b => b.Status == BookingStatus.Accepted),
                allBookings.Count(b => b.Status == BookingStatus.Cancelled),
                allBookings.Count(b => b.Status == BookingStatus.Rejected),
                allBookings.Count(b => b.Status == BookingStatus.PaymentPending)
            },
            BackgroundColors = new[]
            {
                "#10b981", "#f59e0b", "#3b82f6",
                "#6b7280", "#ef4444", "#06b6d4"
            }
        };

        // User role pie
        var roleChart = new PieChartDto
        {
            Labels = new[] { "Students", "Owners", "Admins" },
            Data = new double[]
            {
                allStudents.Count,
                allOwners.Count,
                allUsers.Count - allStudents.Count - allOwners.Count
            },
            BackgroundColors = new[] { "#6C63FF", "#10b981", "#f59e0b" }
        };

        var confirmedCount = allBookings.Count(
            b => b.Status == BookingStatus.Confirmed);

        var summary = new AnalyticsSummaryDto
        {
            TotalUsers = allUsers.Count,
            NewUsersInRange = rangeUsers.Count,
            TotalStudents = allStudents.Count,
            TotalOwners = allOwners.Count,
            VerifiedStudents = allStudents.Count(s => s.IsIdentityVerified),
            TotalProperties = allProperties.Count(p => !p.IsDeleted),
            ApprovedProperties = allProperties.Count(
                                        p => p.IsApproved && !p.IsDeleted),
            PendingProperties = allProperties.Count(
                                        p => !p.IsApproved && !p.IsDeleted),
            TotalBookings = rangeBookings.Count,
            ConfirmedBookings = confirmedCount,
            PendingBookings = allBookings.Count(
                                        b => b.Status == BookingStatus.Pending),
            TotalRevenue = totalRevenue,
            TotalCommission = totalCommission,
            AverageBookingValue = rangeBookings.Count == 0 ? 0
                                     : rangeBookings.Average(b => b.TotalAmount),
            TotalReviews = allReviews.Count,
            AveragePlatformRating = allReviews.Count == 0 ? 0
                                     : allReviews.Average(r => r.OverallRating),
            TotalReports = allReports.Count,
            OpenReports = allReports.Count(
                                        r => r.Status == ReportStatus.Open),
            UserGrowthPct = CalcGrowth(rangeUsers.Count, prevUsers),
            BookingGrowthPct = CalcGrowth(rangeBookings.Count, prevBookings),
            RevenueGrowthPct = CalcGrowth(
                                        (double)totalRevenue,
                                        (double)prevRevenue),
            DailyActivityChart = dailyChart,
            BookingStatusChart = statusChart,
            UserRoleChart = roleChart,
            DateRange = range
        };

        _cache.Set(cacheKey, summary, CacheTtl);
        return ServiceResult<AnalyticsSummaryDto>.Success(summary);
    }

    // ── GetUserAnalyticsAsync ─────────────────────────────────────────────────

    public async Task<ServiceResult<UserAnalyticsDto>> GetUserAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default)
    {
        var cacheKey =
            $"{CacheKeyPrefix}users:{range.From:yyyyMMdd}:{range.To:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out UserAnalyticsDto? cached))
            return ServiceResult<UserAnalyticsDto>.Success(cached!);

        var allUsers = await _identityService.GetAllUsersAsync(ct);
        var allStudents = await _uow.Repository<Student>()
            .GetAllAsync(ct);
        var allOwners = await _uow.Repository<Owner>()
            .GetAllAsync(ct);
        var verifications = await _uow.Repository<IdentityVerification>()
            .GetAllAsync(ct);
        var universities = await _uow.Universities.GetActiveAsync(ct);

        var rangeUsers = allUsers.Where(u => u.CreatedAt >= range.From &&
                                              u.CreatedAt <= range.To)
                                  .OrderByDescending(u => u.CreatedAt)
                                  .ToList();

        // Registration trend chart
        var regTrend = BuildTrendChart(
            allUsers.Select(u => u.CreatedAt).ToList(),
            range, "Registrations", "#6C63FF");

        // Verification trend chart
        var verTrend = BuildTrendChart(
            verifications.Select(v => v.CreatedAt).ToList(),
            range, "Verifications Submitted", "#10b981");

        // Role distribution pie
        var rolePie = new PieChartDto
        {
            Labels = new[] { "Students", "Owners", "Admins" },
            Data = new double[]
            {
                allStudents.Count,
                allOwners.Count,
                Math.Max(0, allUsers.Count - allStudents.Count - allOwners.Count)
            },
            BackgroundColors = new[] { "#6C63FF", "#10b981", "#f59e0b" }
        };

        // Verification status pie
        var verPie = new PieChartDto
        {
            Labels = new[] { "Pending", "Approved", "Rejected" },
            Data = new double[]
            {
                verifications.Count(v => v.Status == VerificationStatus.Pending),
                verifications.Count(v => v.Status == VerificationStatus.Approved),
                verifications.Count(v => v.Status == VerificationStatus.Rejected)
            },
            BackgroundColors = new[] { "#f59e0b", "#10b981", "#ef4444" }
        };

        // Top universities
        var topUnis = new List<UniversityStatsRow>();
        foreach (var uni in universities.Take(10))
        {
            var stuCount = allStudents
                .Count(s => s.UniversityId == uni.Id);
            topUnis.Add(new UniversityStatsRow
            {
                UniversityNameEn = uni.NameEn,
                StudentCount = stuCount,
                PropertyCount = 0,   // enriched below
                BookingCount = 0
            });
        }

        // Recent users (last 10 in range)
        var recentUsers = new List<RecentUserRow>();
        foreach (var u in rangeUsers.Take(10))
        {
            var isStudent = allStudents.Any(s => s.UserId == u.Id);
            var student = allStudents.FirstOrDefault(s => s.UserId == u.Id);
            var isOwner = allOwners.Any(o => o.UserId == u.Id);
            var owner = allOwners.FirstOrDefault(o => o.UserId == u.Id);

            recentUsers.Add(new RecentUserRow
            {
                UserId = u.Id,
                FullName = isStudent
                    ? student?.FullName ?? u.UserName ?? ""
                    : owner?.FullName ?? u.UserName ?? "",
                Role = isStudent ? "Student"
                             : isOwner ? "Owner" : "Admin",
                Email = u.Email ?? "",
                IsVerified = isStudent
                    ? student?.IsIdentityVerified ?? false
                    : owner?.IsIdentityVerified ?? false,
                RegisteredAt = u.CreatedAt
            });
        }

        var result = new UserAnalyticsDto
        {
            TotalRegistered = allUsers.Count,
            TotalStudents = allStudents.Count,
            TotalOwners = allOwners.Count,
            TotalAdmins = Math.Max(0,
                allUsers.Count - allStudents.Count - allOwners.Count),
            VerifiedStudents = allStudents.Count(s => s.IsIdentityVerified),
            VerifiedOwners = allOwners.Count(o => o.IsIdentityVerified),
            ActiveUsers = allUsers.Count(u => u.IsActive),
            InactiveUsers = allUsers.Count(u => !u.IsActive),
            NewRegistrations = rangeUsers.Count,
            PendingVerifications = verifications.Count(
                v => v.Status == VerificationStatus.Pending),
            ApprovedVerifications = verifications.Count(
                v => v.Status == VerificationStatus.Approved),
            RejectedVerifications = verifications.Count(
                v => v.Status == VerificationStatus.Rejected),
            RegistrationTrendChart = regTrend,
            UserRoleDistribution = rolePie,
            VerificationStatusChart = verPie,
            VerificationTrendChart = verTrend,
            TopUniversities = topUnis,
            RecentUsers = recentUsers,
            DateRange = range
        };

        _cache.Set(cacheKey, result, CacheTtl);
        return ServiceResult<UserAnalyticsDto>.Success(result);
    }

    // ── GetBookingAnalyticsAsync ──────────────────────────────────────────────

    public async Task<ServiceResult<BookingAnalyticsDto>> GetBookingAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default)
    {
        var cacheKey =
            $"{CacheKeyPrefix}bookings:{range.From:yyyyMMdd}:{range.To:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out BookingAnalyticsDto? cached))
            return ServiceResult<BookingAnalyticsDto>.Success(cached!);

        var allBookings = await _uow.Repository<Booking>()
            .GetAllAsync(
               predicate: null,
                orderBy: null,
                 ct: ct,
                b => b.Student,
                b => b.Property,
                b => b.Property.Area,
                b => b.Property.Owner);

        var rangeBookings = allBookings
            .Where(b => b.CreatedAt >= range.From &&
                        b.CreatedAt <= range.To)
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

        var confirmed = rangeBookings.Count(
            b => b.Status == BookingStatus.Confirmed);
        var pending = rangeBookings.Count(
            b => b.Status == BookingStatus.Pending);
        var accepted = rangeBookings.Count(
            b => b.Status == BookingStatus.Accepted);
        var cancelled = rangeBookings.Count(
            b => b.Status == BookingStatus.Cancelled);
        var rejected = rangeBookings.Count(
            b => b.Status == BookingStatus.Rejected);

        // Average owner response time
        var respondedBookings = rangeBookings
            .Where(b => b.RespondedAt.HasValue).ToList();
        var avgResponseHours = respondedBookings.Count == 0 ? 0
            : respondedBookings.Average(b =>
                (b.RespondedAt!.Value - b.RequestedAt).TotalHours);

        // Booking trend chart
        var trendChart = BuildTrendChart(
            rangeBookings.Select(b => b.CreatedAt).ToList(),
            range, "Booking Requests", "#6C63FF");

        // Status pie
        var statusPie = new PieChartDto
        {
            Labels = new[]
            {
                "Confirmed", "Pending", "Accepted",
                "Cancelled", "Rejected"
            },
            Data = new double[]
            {
                confirmed, pending, accepted, cancelled, rejected
            },
            BackgroundColors = new[]
            {
                "#10b981", "#f59e0b", "#3b82f6", "#6b7280", "#ef4444"
            }
        };

        // Property type bookings pie
        var typeGroups = rangeBookings
            .GroupBy(b => b.Property?.PropertyType.ToString() ?? "Unknown")
            .OrderByDescending(g => g.Count())
            .ToList();

        var typePie = new PieChartDto
        {
            Labels = typeGroups.Select(g => g.Key).ToList(),
            Data = typeGroups
                .Select(g => (double)g.Count()).ToList(),
            BackgroundColors = ChartColors
                .Take(typeGroups.Count).ToList()
        };

        // Top booked properties
        var topProperties = rangeBookings
            .GroupBy(b => b.PropertyId)
            .Select(g =>
            {
                var first = g.First();
                return new TopBookedPropertyRow
                {
                    PropertyId = g.Key,
                    Title = first.Property?.Title ?? "—",
                    AreaNameEn = first.Property?.Area?.NameEn ?? "—",
                    OwnerName = first.Property?.Owner?.FullName ?? "—",
                    TotalBookings = g.Count(),
                    ConfirmedBookings = g.Count(
                        b => b.Status == BookingStatus.Confirmed),
                    TotalRevenue = g.Where(
                        b => b.Status == BookingStatus.Confirmed)
                        .Sum(b => b.TotalAmount)
                };
            })
            .OrderByDescending(r => r.TotalBookings)
            .Take(10)
            .ToList();

        // Top booking areas
        var totalRangeBookings = rangeBookings.Count;
        var topAreas = rangeBookings
            .GroupBy(b => b.Property?.Area?.NameEn ?? "Unknown")
            .Select(g => new AreaBookingRow
            {
                AreaNameEn = g.Key,
                BookingCount = g.Count(),
                TotalRevenue = g.Where(
                    b => b.Status == BookingStatus.Confirmed)
                    .Sum(b => b.TotalAmount),
                Percentage = totalRangeBookings == 0 ? 0
                    : Math.Round((double)g.Count() /
                                 totalRangeBookings * 100, 1)
            })
            .OrderByDescending(r => r.BookingCount)
            .Take(8)
            .ToList();

        // Recent bookings
        var recentBookings = rangeBookings.Take(15).Select(b =>
        {
            var (badge, display) = GetStatusBadge(b.Status);
            return new RecentBookingRow
            {
                BookingId = b.Id,
                StudentName = b.Student?.FullName ?? "—",
                PropertyTitle = b.Property?.Title ?? "—",
                Status = b.Status,
                StatusDisplay = display,
                StatusBadge = badge,
                Amount = b.TotalAmount,
                RequestedAt = b.RequestedAt
            };
        }).ToList();

        var result = new BookingAnalyticsDto
        {
            TotalBookings = rangeBookings.Count,
            PendingBookings = pending,
            AcceptedBookings = accepted,
            ConfirmedBookings = confirmed,
            CancelledBookings = cancelled,
            RejectedBookings = rejected,
            ConversionRate = rangeBookings.Count == 0 ? 0
                : Math.Round((double)confirmed / rangeBookings.Count * 100, 1),
            AcceptanceRate = (pending + accepted + confirmed) == 0 ? 0
                : Math.Round((double)(accepted + confirmed) /
                             (pending + accepted + confirmed) * 100, 1),
            CancellationRate = rangeBookings.Count == 0 ? 0
                : Math.Round((double)cancelled / rangeBookings.Count * 100, 1),
            AvgResponseHours = Math.Round(avgResponseHours, 1),
            BookingTrendChart = trendChart,
            BookingStatusChart = statusPie,
            DailyBookingsChart = trendChart,
            PropertyTypeBookingsChart = typePie,
            TopBookedProperties = topProperties,
            TopBookingAreas = topAreas,
            RecentBookings = recentBookings,
            DateRange = range
        };

        _cache.Set(cacheKey, result, CacheTtl);
        return ServiceResult<BookingAnalyticsDto>.Success(result);
    }

    // ── GetRevenueAnalyticsAsync ──────────────────────────────────────────────

    public async Task<ServiceResult<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default)
    {
        var cacheKey =
            $"{CacheKeyPrefix}revenue:{range.From:yyyyMMdd}:{range.To:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out RevenueAnalyticsDto? cached))
            return ServiceResult<RevenueAnalyticsDto>.Success(cached!);

        var allPayments = await _uow.Repository<Payment>()
            .GetAllAsync(
            null,
            null,
            ct,
                p => p.Booking,
                p => p.Booking.Student,
                p => p.Booking.Property,
                p => p.Booking.Property.Owner);

        var allBookings = await _uow.Repository<Booking>()
            .GetAllAsync(
            predicate: null,
            orderBy: null,
            ct,
                b => b.Property,
                b => b.Property.Owner);

        var rangePayments = allPayments
            .Where(p => p.PaidAt >= range.From &&
                        p.PaidAt <= range.To &&
                        p.Status == PaymentStatus.Success)
            .OrderByDescending(p => p.PaidAt)
            .ToList();

        // Previous period revenue
        var prevFrom = range.From.AddDays(-range.TotalDays);
        var prevRevenue = allPayments
            .Where(p => p.PaidAt >= prevFrom &&
                        p.PaidAt < range.From &&
                        p.Status == PaymentStatus.Success)
            .Sum(p => p.Amount);

        var totalRevenue = rangePayments.Sum(p => p.Amount);

        // Commission from confirmed bookings in range
        var rangeConfirmed = allBookings.Where(b =>
            b.Status == BookingStatus.Confirmed &&
            b.CreatedAt >= range.From &&
            b.CreatedAt <= range.To).ToList();

        var totalCommission = rangeConfirmed.Sum(b => b.CommissionAmount ?? 0);
        var netOwnerPayouts = totalRevenue - totalCommission;

        // Payment method breakdown
        var totalPaymentCount = rangePayments.Count;
        var byMethod = rangePayments
            .GroupBy(p => p.PaymentMethod.ToString())
            .Select(g => new PaymentMethodRow
            {
                Method = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = totalPaymentCount == 0 ? 0
                    : Math.Round((double)g.Count() / totalPaymentCount * 100, 1)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        // Revenue trend chart
        var revenueTrend = BuildRevenueTrendChart(rangePayments, range);

        // Revenue vs Commission chart
        var revenueVsCommission = BuildRevenueVsCommissionChart(
            allBookings, range);

        // Payment method pie
        var methodPie = new PieChartDto
        {
            Labels = byMethod.Select(m => m.Method).ToList(),
            Data = byMethod.Select(m => (double)m.TotalAmount).ToList(),
            BackgroundColors = ChartColors.Take(byMethod.Count).ToList()
        };

        // Monthly comparison (current year)
        var monthlyBreakdown = BuildMonthlyBreakdown(allPayments, allBookings);

        // Top owners by revenue
        var topOwners = rangeConfirmed
            .GroupBy(b => b.Property.OwnerId)
            .Select(g =>
            {
                var first = g.First();
                var gross = g.Sum(b => b.TotalAmount);
                var comm = g.Sum(b => b.CommissionAmount ?? 0);
                return new TopOwnerRevenueRow
                {
                    OwnerId = g.Key,
                    OwnerName = first.Property.Owner?.FullName ?? "—",
                    PropertyCount = g.Select(b => b.PropertyId).Distinct().Count(),
                    ConfirmedBookings = g.Count(),
                    GrossRevenue = gross,
                    CommissionPaid = comm,
                    NetPayout = gross - comm
                };
            })
            .OrderByDescending(r => r.GrossRevenue)
            .Take(10)
            .ToList();

        // Recent transactions
        var recentTx = rangePayments.Take(15).Select(p => new RecentTransactionRow
        {
            PaymentId = p.Id,
            BookingId = p.BookingId,
            StudentName = p.Booking?.Student?.FullName ?? "—",
            PropertyTitle = p.Booking?.Property?.Title ?? "—",
            Method = p.PaymentMethod.ToString(),
            Amount = p.Amount,
            TransactionRef = p.TransactionReference,
            PaidAt = p.PaidAt ?? DateTime.UtcNow
        }).ToList();

        var result = new RevenueAnalyticsDto
        {
            TotalRevenue = totalRevenue,
            TotalCommission = totalCommission,
            NetOwnerPayouts = netOwnerPayouts,
            AverageBookingValue = rangePayments.Count == 0 ? 0
                : rangePayments.Average(p => p.Amount),
            PreviousPeriodRevenue = prevRevenue,
            ByPaymentMethod = byMethod,
            RevenueTrendChart = revenueTrend,
            RevenueVsCommissionChart = revenueVsCommission,
            PaymentMethodChart = methodPie,
            MonthlyComparisonChart = BuildMonthlyComparisonChart(allPayments),
            TopOwnersByRevenue = topOwners,
            RecentTransactions = recentTx,
            MonthlyBreakdown = monthlyBreakdown,
            DateRange = range
        };

        _cache.Set(cacheKey, result, CacheTtl);
        return ServiceResult<RevenueAnalyticsDto>.Success(result);
    }

    // ── GetPropertyAnalyticsAsync ─────────────────────────────────────────────

    public async Task<ServiceResult<PropertyAnalyticsDto>> GetPropertyAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default)
    {
        var cacheKey =
            $"{CacheKeyPrefix}properties:{range.From:yyyyMMdd}:{range.To:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out PropertyAnalyticsDto? cached))
            return ServiceResult<PropertyAnalyticsDto>.Success(cached!);

        var allProperties = await _uow.Repository<Property>()
     .GetAllAsync(
            null,
            null,
            ct,
         p => p.Area,
         p => p.Owner,
         p => p.Images,
         p => p.PropertyAmenities);

        var allBookings = await _uow.Repository<Booking>()
            .GetAllAsync(ct);

        var allReviews = await _uow.Repository<Review>()
            .GetAllAsync(ct);

        var active = allProperties.Where(p => !p.IsDeleted).ToList();
        var approved = active.Where(p => p.IsApproved).ToList();
        var pending = active.Where(p => !p.IsApproved).ToList();
        var inRange = active.Where(p => p.CreatedAt >= range.From &&
                                          p.CreatedAt <= range.To).ToList();

        // Listing trend chart
        var listingTrend = BuildTrendChart(
            inRange.Select(p => p.CreatedAt).ToList(),
            range, "New Listings", "#6C63FF");

        // Property type breakdown
        var totalCount = active.Count;
        var typeRows = active
            .GroupBy(p => p.PropertyType.ToString())
            .Select(g => new PropertyTypeRow
            {
                TypeDisplay = g.Key,
                Count = g.Count(),
                Percentage = totalCount == 0 ? 0
                    : Math.Round((double)g.Count() / totalCount * 100, 1),
                AvgPrice = g.Average(p => p.PricePerMonth),
                AvgRating = g.Where(p => p.AverageRating > 0).Any()
                    ? Math.Round(g.Where(p => p.AverageRating > 0)
                                  .Average(p => (double)p.AverageRating), 2)
                    : 0
            })
            .OrderByDescending(r => r.Count)
            .ToList();

        // Type pie chart
        var typePie = new PieChartDto
        {
            Labels = typeRows.Select(t => t.TypeDisplay).ToList(),
            Data = typeRows.Select(t => (double)t.Count).ToList(),
            BackgroundColors = ChartColors.Take(typeRows.Count).ToList()
        };

        // Approval status pie
        var approvalPie = new PieChartDto
        {
            Labels = new[] { "Approved", "Pending", "Deleted" },
            Data = new double[]
            {
                approved.Count,
                pending.Count,
                allProperties.Count(p => p.IsDeleted)
            },
            BackgroundColors = new[] { "#10b981", "#f59e0b", "#ef4444" }
        };

        // Average price by area chart
        var areaGroups = approved
            .GroupBy(p => p.Area?.NameEn ?? "Unknown")
            .Select(g => (Area: g.Key,
                         AvgPrice: g.Average(p => (double)p.PricePerMonth)))
            .OrderByDescending(x => x.AvgPrice)
            .Take(8)
            .ToList();

        var avgPriceChart = new ChartDataDto
        {
            Labels = areaGroups.Select(x => x.Area).ToList(),
            Datasets = new[]
            {
                new ChartDatasetDto
                {
                    Label           = "Avg Price (EGP/month)",
                    Data            = areaGroups.Select(x => x.AvgPrice).ToList(),
                    BackgroundColor = "#6C63FF",
                    BorderColor     = "#5b52d4",
                    Type            = "bar"
                }
            }
        };

        // Occupancy rate chart (% of approved properties currently booked)
        var occupancyChart = BuildOccupancyChart(approved, allBookings);

        // Area breakdown
        var areaRows = approved
            .GroupBy(p => p.Area?.NameEn ?? "Unknown")
            .Select(g =>
            {
                var propIds = g.Select(p => p.Id).ToHashSet();
                var areaBookings = allBookings.Count(
                    b => propIds.Contains(b.PropertyId));
                var occupied = g.Count(
                    p => !p.IsAvailable);

                return new PropertyAreaRow
                {
                    AreaNameEn = g.Key,
                    PropertyCount = g.Count(),
                    BookingCount = areaBookings,
                    AvgPrice = g.Average(p => p.PricePerMonth),
                    OccupancyRate = g.Count() == 0 ? 0
                        : Math.Round((double)occupied / g.Count() * 100, 1)
                };
            })
            .OrderByDescending(r => r.PropertyCount)
            .ToList();

        // Top rated properties
        var topRated = approved
            .Where(p => p.AverageRating > 0)
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.Reviews.Count())
            .Take(10)
            .Select(p =>
            {
                var bookingCount = allBookings.Count(b => b.PropertyId == p.Id);
                return new TopRatedPropertyRow
                {
                    PropertyId = p.Id,
                    Title = p.Title,
                    AreaNameEn = p.Area?.NameEn ?? "—",
                    OwnerName = p.Owner?.FullName ?? "—",
                    PricePerMonth = p.PricePerMonth,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.Reviews.Count(),
                    TotalBookings = bookingCount
                };
            })
            .ToList();

        // Pending approval queue
        var pendingQueue = pending
            .OrderBy(p => p.CreatedAt)
            .Take(10)
            .Select(p =>
            {
                var waitDays = (DateTime.UtcNow - p.CreatedAt).TotalDays;
                return new PendingPropertyRow
                {
                    PropertyId = p.Id,
                    Title = p.Title,
                    OwnerName = p.Owner?.FullName ?? "—",
                    AreaNameEn = p.Area?.NameEn ?? "—",
                    TypeDisplay = p.PropertyType.ToString(),
                    PricePerMonth = p.PricePerMonth,
                    SubmittedAt = p.CreatedAt,
                    WaitingTime = waitDays < 1
                        ? $"{(int)(waitDays * 24)}h"
                        : $"{(int)waitDays}d"
                };
            })
            .ToList();

        var result = new PropertyAnalyticsDto
        {
            TotalProperties = active.Count,
            ApprovedProperties = approved.Count,
            PendingApproval = pending.Count,
            UnavailableProperties = approved.Count(p => !p.IsAvailable),
            DeletedProperties = allProperties.Count(p => p.IsDeleted),
            AveragePlatformRating = approved.Count == 0 ? 0
                : Math.Round(approved.Average(p => (double)p.AverageRating), 2),
            NewPropertiesInRange = inRange.Count,
            ByType = typeRows,
            ByArea = areaRows,
            ListingTrendChart = listingTrend,
            PropertyTypeChart = typePie,
            ApprovalStatusChart = approvalPie,
            AvgPriceByAreaChart = avgPriceChart,
            OccupancyChart = occupancyChart,
            TopRatedProperties = topRated,
            PendingApprovalQueue = pendingQueue,
            DateRange = range
        };

        _cache.Set(cacheKey, result, CacheTtl);
        return ServiceResult<PropertyAnalyticsDto>.Success(result);
    }

    // ── InvalidateCache ───────────────────────────────────────────────────────

    public void InvalidateCache()
    {
        // IMemoryCache doesn't support prefix removal —
        // use a generation counter approach or just let TTL expire.
        // For a production upgrade, switch to IDistributedCache with Redis
        // and use key scanning.
        _logger.LogDebug("Analytics cache invalidated");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PRIVATE CHART BUILDERS
    // ══════════════════════════════════════════════════════════════════════════

    private ChartDataDto BuildDailyActivityChart(
       IReadOnlyList<IdentityUserDto> rangeUsers,
        IReadOnlyList<Booking> rangeBookings,
        IReadOnlyList<Payment> rangePayments,
        DateRangeDto range)
    {
        var labels = new List<string>();
        var regData = new List<double>();
        var bookData = new List<double>();
        var payData = new List<double>();

        // Group by day for short ranges, by week for longer ranges
        bool groupByWeek = range.TotalDays > 60;

        var current = range.From.Date;
        while (current <= range.To.Date)
        {
            var next = groupByWeek
                ? current.AddDays(7)
                : current.AddDays(1);

            labels.Add(groupByWeek
                ? $"Week {current:MMM dd}"
                : current.ToString("MMM dd"));

            regData.Add(rangeUsers.Count(u =>
                u.CreatedAt >= current && u.CreatedAt < next));
            bookData.Add(rangeBookings.Count(b =>
                b.CreatedAt >= current && b.CreatedAt < next));
            payData.Add(rangePayments.Count(p =>
                p.PaidAt >= current && p.PaidAt < next));

            current = next;
        }

        return new ChartDataDto
        {
            Labels = labels,
            Datasets = new[]
            {
                new ChartDatasetDto
                {
                    Label           = "New Users",
                    Data            = regData,
                    BackgroundColor = "rgba(108,99,255,0.15)",
                    BorderColor     = "#6C63FF",
                    Type            = "line",
                    Fill            = true
                },
                new ChartDatasetDto
                {
                    Label           = "Bookings",
                    Data            = bookData,
                    BackgroundColor = "rgba(16,185,129,0.15)",
                    BorderColor     = "#10b981",
                    Type            = "line",
                    Fill            = true
                },
                new ChartDatasetDto
                {
                    Label           = "Payments",
                    Data            = payData,
                    BackgroundColor = "rgba(245,158,11,0.15)",
                    BorderColor     = "#f59e0b",
                    Type            = "line",
                    Fill            = true
                }
            }
        };
    }

    private ChartDataDto BuildTrendChart(
        IReadOnlyList<DateTime> dates,
        DateRangeDto range,
        string label,
        string color)
    {
        var labels = new List<string>();
        var data = new List<double>();
        bool weekly = range.TotalDays > 60;

        var current = range.From.Date;
        while (current <= range.To.Date)
        {
            var next = weekly ? current.AddDays(7) : current.AddDays(1);
            labels.Add(weekly ? $"Wk {current:MMM dd}" : current.ToString("MMM dd"));
            data.Add(dates.Count(d => d >= current && d < next));
            current = next;
        }

        return new ChartDataDto
        {
            Labels = labels,
            Datasets = new[]
            {
                new ChartDatasetDto
                {
                    Label           = label,
                    Data            = data,
                    BackgroundColor = color + "26",  // 15% opacity
                    BorderColor     = color,
                    Type            = "line",
                    Fill            = true
                }
            }
        };
    }

    private ChartDataDto BuildRevenueTrendChart(
        IReadOnlyList<Payment> payments,
        DateRangeDto range)
    {
        var labels = new List<string>();
        var data = new List<double>();
        bool weekly = range.TotalDays > 60;

        var current = range.From.Date;
        while (current <= range.To.Date)
        {
            var next = weekly ? current.AddDays(7) : current.AddDays(1);
            labels.Add(weekly ? $"Wk {current:MMM dd}" : current.ToString("MMM dd"));
            data.Add((double)payments
                .Where(p => p.PaidAt >= current && p.PaidAt < next)
                .Sum(p => p.Amount));
            current = next;
        }

        return new ChartDataDto
        {
            Labels = labels,
            Datasets = new[]
            {
                new ChartDatasetDto
                {
                    Label           = "Revenue (EGP)",
                    Data            = data,
                    BackgroundColor = "rgba(16,185,129,0.15)",
                    BorderColor     = "#10b981",
                    Type            = "bar"
                }
            }
        };
    }

    private ChartDataDto BuildRevenueVsCommissionChart(
        IReadOnlyList<Booking> allBookings,
        DateRangeDto range)
    {
        // Monthly — always 12 months for this chart
        var labels = new List<string>();
        var revenue = new List<double>();
        var commData = new List<double>();

        var now = DateTime.UtcNow;
        for (int i = 11; i >= 0; i--)
        {
            var mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            labels.Add(mStart.ToString("MMM yy"));

            var mConfirmed = allBookings.Where(b =>
                b.Status == BookingStatus.Confirmed &&
                b.CreatedAt >= mStart &&
                b.CreatedAt <= mEnd).ToList();

            revenue.Add((double)mConfirmed.Sum(b => b.TotalAmount));
            commData.Add((double)mConfirmed.Sum(b => b.CommissionAmount ?? 0));
        }

        return new ChartDataDto
        {
            Labels = labels,
            Datasets = new[]
            {
                new ChartDatasetDto
                {
                    Label           = "Gross Revenue",
                    Data            = revenue,
                    BackgroundColor = "rgba(16,185,129,0.7)",
                    BorderColor     = "#10b981",
                    Type            = "bar"
                },
                new ChartDatasetDto
                {
                    Label           = "Commission",
                    Data            = commData,
                    BackgroundColor = "rgba(108,99,255,0.7)",
                    BorderColor     = "#6C63FF",
                    Type            = "bar"
                }
            }
        };
    }

    private ChartDataDto BuildMonthlyComparisonChart(
        IReadOnlyList<Payment> allPayments)
    {
        var now = DateTime.UtcNow;
        var labels = new List<string>();
        var thisYearData = new List<double>();
        var lastYearData = new List<double>();

        for (int m = 1; m <= 12; m++)
        {
            labels.Add(new DateTime(now.Year, m, 1).ToString("MMM"));

            thisYearData.Add((double)allPayments
                .Where(p => p.PaidAt?.Year == now.Year &&
                             p.PaidAt?.Month == m &&
                             p.Status == PaymentStatus.Success)
                .Sum(p => p.Amount));

            lastYearData.Add((double)allPayments
                .Where(p => p.PaidAt?.Year == now.Year - 1 &&
                             p.PaidAt?.Month == m &&
                             p.Status == PaymentStatus.Success)
                .Sum(p => p.Amount));
        }

        return new ChartDataDto
        {
            Labels = labels,
            Datasets = new[]
            {
                new ChartDatasetDto
                {
                    Label           = $"{now.Year}",
                    Data            = thisYearData,
                    BackgroundColor = "rgba(16,185,129,0.15)",
                    BorderColor     = "#10b981",
                    Type            = "line"
                },
                new ChartDatasetDto
                {
                    Label           = $"{now.Year - 1}",
                    Data            = lastYearData,
                    BackgroundColor = "rgba(107,114,128,0.15)",
                    BorderColor     = "#6b7280",
                    Type            = "line"
                }
            }
        };
    }

    private ChartDataDto BuildOccupancyChart(
        IReadOnlyList<Property> approved,
        IReadOnlyList<Booking> allBookings)
    {
        // Monthly occupancy rate for last 12 months
        var now = DateTime.UtcNow;
        var labels = new List<string>();
        var data = new List<double>();

        for (int i = 11; i >= 0; i--)
        {
            var mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            labels.Add(mStart.ToString("MMM yy"));

            var confirmedInMonth = allBookings.Count(b =>
                b.Status == BookingStatus.Confirmed &&
                b.CreatedAt >= mStart &&
                b.CreatedAt <= mEnd);

            var rate = approved.Count == 0 ? 0
                : Math.Round((double)confirmedInMonth / approved.Count * 100, 1);

            data.Add(rate);
        }

        return new ChartDataDto
        {
            Labels = labels,
            Datasets = new[]
            {
                new ChartDatasetDto
                {
                    Label           = "Occupancy Rate (%)",
                    Data            = data,
                    BackgroundColor = "rgba(59,130,246,0.15)",
                    BorderColor     = "#3b82f6",
                    Type            = "line",
                    Fill            = true
                }
            }
        };
    }

    private IReadOnlyList<MonthlyRevenueRow> BuildMonthlyBreakdown(
        IReadOnlyList<Payment> allPayments,
        IReadOnlyList<Booking> allBookings)
    {
        var now = DateTime.UtcNow;
        var rows = new List<MonthlyRevenueRow>();
        double? prevRevenue = null;

        for (int i = 11; i >= 0; i--)
        {
            var mStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            var mPayments = allPayments.Where(p =>
                p.PaidAt >= mStart && p.PaidAt <= mEnd &&
                p.Status == PaymentStatus.Success).ToList();

            var mConfirmed = allBookings.Where(b =>
                b.Status == BookingStatus.Confirmed &&
                b.CreatedAt >= mStart &&
                b.CreatedAt <= mEnd).ToList();

            var gross = mPayments.Sum(p => p.Amount);
            var comm = mConfirmed.Sum(b => b.CommissionAmount ?? 0);
            var growth = prevRevenue.HasValue && prevRevenue.Value > 0
                ? Math.Round(((double)gross - prevRevenue.Value) /
                              prevRevenue.Value * 100, 1)
                : 0;

            rows.Add(new MonthlyRevenueRow
            {
                MonthLabel = mStart.ToString("MMMM yyyy"),
                BookingCount = mConfirmed.Count,
                GrossRevenue = gross,
                Commission = comm,
                NetPayouts = gross - comm,
                GrowthPct = growth
            });

            prevRevenue = (double)gross;
        }

        return rows;
    }

    // ── Private Utilities ─────────────────────────────────────────────────────

    private static double CalcGrowth(double current, double previous)
        => previous == 0 ? 0
            : Math.Round((current - previous) / previous * 100, 1);

    private static (string Badge, string Display) GetStatusBadge(BookingStatus status)
        => status switch
        {
            BookingStatus.Confirmed => ("bg-success", "Confirmed"),
            BookingStatus.Pending => ("bg-warning text-dark", "Pending"),
            BookingStatus.Accepted => ("bg-primary", "Accepted"),
            BookingStatus.Cancelled => ("bg-secondary", "Cancelled"),
            BookingStatus.Rejected => ("bg-danger", "Rejected"),
            BookingStatus.PaymentPending => ("bg-info", "Payment Pending"),
            _ => ("bg-secondary", status.ToString())
        };
}
