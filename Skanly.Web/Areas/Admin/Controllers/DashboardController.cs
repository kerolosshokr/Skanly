// Skanly.Web/Areas/Admin/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Analytics.DTOs;
using Skanly.Application.Features.Analytics.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IAnalyticsService _analytics;

    public DashboardController(
        IUnitOfWork uow,
        IAnalyticsService analytics)
    {
        _uow = uow;
        _analytics = analytics;
    }

    [HttpGet("/Admin/Dashboard")]
    [HttpGet("/Admin")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var range = DateRangeDto.Last30Days();
        var result = await _analytics.GetSummaryAsync(range, ct);

        // Badge counts for sidebar
        ViewBag.PendingProperties =
            await _uow.Repository<Property>()
                .CountAsync(p => !p.IsApproved && !p.IsDeleted, ct);

        ViewBag.PendingVerifications =
            await _uow.Repository<IdentityVerification>()
                .CountAsync(v => v.Status == VerificationStatus.Pending, ct);

        ViewBag.OpenReports =
            await _uow.Repository<Report>()
                .CountAsync(r => r.Status == ReportStatus.Open, ct);

        return View(result.Data);
    }
}