// Skanly.Web/Areas/Admin/Controllers/AnalyticsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Analytics.DTOs;
using Skanly.Application.Features.Analytics.Interfaces;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static DateRangeDto ParseRange(string? preset, DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue)
            return new DateRangeDto { From = from.Value, To = to.Value };

        return preset switch
        {
            "7d" => DateRangeDto.Last7Days(),
            "30d" => DateRangeDto.Last30Days(),
            "90d" => DateRangeDto.Last90Days(),
            "thisMonth" => DateRangeDto.ThisMonth(),
            "thisYear" => DateRangeDto.ThisYear(),
            "lastYear" => DateRangeDto.LastYear(),
            _ => DateRangeDto.Last30Days()
        };
    }

    // ── Main Dashboard ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        string? preset = "30d",
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = ParseRange(preset, from, to);
        var result = await _analytics.GetSummaryAsync(range, ct);

        ViewBag.Preset = preset ?? "30d";

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── AJAX: summary chart data ───────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> SummaryData(
        string? preset = "30d",
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = ParseRange(preset, from, to);
        var result = await _analytics.GetSummaryAsync(range, ct);

        return result.IsSuccess ? Ok(result.Data) : BadRequest();
    }

    // ── Users Page ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Users(
        string? preset = "30d",
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = ParseRange(preset, from, to);
        var result = await _analytics.GetUserAnalyticsAsync(range, ct);

        ViewBag.Preset = preset ?? "30d";

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Bookings Page ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Bookings(
        string? preset = "30d",
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = ParseRange(preset, from, to);
        var result = await _analytics.GetBookingAnalyticsAsync(range, ct);

        ViewBag.Preset = preset ?? "30d";

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Revenue Page ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Revenue(
        string? preset = "30d",
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = ParseRange(preset, from, to);
        var result = await _analytics.GetRevenueAnalyticsAsync(range, ct);

        ViewBag.Preset = preset ?? "30d";

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Properties Page ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Properties(
        string? preset = "30d",
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = ParseRange(preset, from, to);
        var result = await _analytics.GetPropertyAnalyticsAsync(range, ct);

        ViewBag.Preset = preset ?? "30d";

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── AJAX: Invalidate cache ─────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult InvalidateCache()
    {
        _analytics.InvalidateCache();
        return Ok(new { message = "Cache invalidated." });
    }
}