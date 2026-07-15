// Skanly.Application/Features/Analytics/Interfaces/IAnalyticsService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Analytics.DTOs;

namespace Skanly.Application.Features.Analytics.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Top-level summary for the main analytics dashboard.
    /// Cached — refreshes every 15 minutes.
    /// </summary>
    Task<ServiceResult<AnalyticsSummaryDto>> GetSummaryAsync(
        DateRangeDto range,
        CancellationToken ct = default);

    Task<ServiceResult<UserAnalyticsDto>> GetUserAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default);

    Task<ServiceResult<BookingAnalyticsDto>> GetBookingAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default);

    Task<ServiceResult<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default);

    Task<ServiceResult<PropertyAnalyticsDto>> GetPropertyAnalyticsAsync(
        DateRangeDto range,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidates the analytics cache (called after major write operations).
    /// </summary>
    void InvalidateCache();
}