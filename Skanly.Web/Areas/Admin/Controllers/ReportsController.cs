// Skanly.Web/Areas/Admin/Controllers/ReportsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Reports.DTOs;
using Skanly.Application.Features.Reports.Interfaces;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;
using System.Security.Claims;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string AdminId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Index ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        ReportStatus? status = null,
        ReportType? type = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var result = await _reportService.GetAllAsync(
            page, 20, status, type, search, ct);

        var summary = await _reportService.GetSummaryAsync(ct);

        ViewBag.StatusFilter = status;
        ViewBag.TypeFilter = type;
        ViewBag.Search = search;
        ViewBag.Summary = summary.Data;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Report Detail ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await _reportService
            .GetByIdAsync(AdminId, id, isAdmin: true, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── Mark Under Investigation (AJAX) ───────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Investigate(
        int id,
        string notes,
        CancellationToken ct)
    {
        var dto = new ResolveReportDto
        {
            ReportId = id,
            NewStatus = ReportStatus.UnderInvestigation,
            Resolution = notes
        };

        var result = await _reportService.ResolveAsync(AdminId, dto, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }

    // ── Resolve (AJAX + form) ─────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(
        ResolveReportDto dto,
        CancellationToken ct)
    {
        var result = await _reportService.ResolveAsync(AdminId, dto, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return result.IsSuccess
                ? Json(new { success = true })
                : Json(new { success = false, message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] =
                $"Report #{dto.ReportId} has been {dto.NewStatus}.";
        }

        return RedirectToAction(nameof(Details), new { id = dto.ReportId });
    }

    // ── Dismiss (AJAX quick action) ───────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(
        int id,
        string reason,
        CancellationToken ct)
    {
        var dto = new ResolveReportDto
        {
            ReportId = id,
            NewStatus = ReportStatus.Dismissed,
            Resolution = reason
        };

        var result = await _reportService.ResolveAsync(AdminId, dto, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }
}