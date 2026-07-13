// Skanly.Web/Controllers/ReportsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Reports.DTOs;
using Skanly.Application.Features.Reports.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Create Report ─────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Create(
        int? propertyId = null,
        string? userId = null)
    {
        var dto = new CreateReportDto
        {
            ReportedPropertyId = propertyId,
            ReportedUserId = userId
        };

        ViewBag.PropertyId = propertyId;
        ViewBag.UserId = userId;
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateReportDto dto,
        CancellationToken ct)
    {
        var result = await _reportService.CreateAsync(UserId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            ViewBag.PropertyId = dto.ReportedPropertyId;
            ViewBag.UserId = dto.ReportedUserId;
            return View(dto);
        }

        TempData["Success"] =
            "Your report has been submitted. " +
            "Our team will review it within 24–48 hours.";

        return RedirectToAction(nameof(MyReports));
    }

    // ── My Reports List ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> MyReports(
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _reportService
            .GetByReporterAsync(UserId, page, 10, ct);

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Report Detail (reporter view) ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await _reportService.GetByIdAsync(UserId, id, false, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(MyReports));
        }

        return View(result.Data);
    }

    // ── AJAX: Quick report from property detail or user profile ───────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickReport(
        CreateReportDto dto,
        CancellationToken ct)
    {
        var result = await _reportService.CreateAsync(UserId, dto, ct);

        return result.IsSuccess
            ? Json(new
            {
                success = true,
                message = "Report submitted. Thank you."
            })
            : Json(new
            {
                success = false,
                message = result.ErrorMessage
            });
    }
}