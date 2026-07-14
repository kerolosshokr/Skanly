// Skanly.Web/Areas/Admin/Controllers/VerificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Verification.DTOs;
using Skanly.Application.Features.Verification.Interfaces;
using Skanly.Domain.Enums;
using System.Security.Claims;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class VerificationsController : Controller
{
    private readonly IVerificationService _verificationService;

    public VerificationsController(
        IVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    private string AdminId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Queue Index ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        VerificationStatus? status = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var result = await _verificationService.GetAllAsync(
            page, 20, status, search, ct);

        var summary = await _verificationService.GetSummaryAsync(ct);

        ViewBag.StatusFilter = status;
        ViewBag.Search = search;
        ViewBag.Summary = summary.Data;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Review Detail ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Review(
        int id,
        CancellationToken ct)
    {
        var result = await _verificationService.GetByIdAsync(id, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── Approve (AJAX) ────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        ReviewVerificationDto dto,
        CancellationToken ct)
    {
        dto.Decision = VerificationStatus.Approved;

        var result = await _verificationService.ReviewAsync(AdminId, dto, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return result.IsSuccess
                ? Json(new { success = true })
                : Json(new
                {
                    success = false,
                    message = result.ErrorMessage
                });

        if (result.IsSuccess)
            TempData["Success"] = "Identity verified successfully.";
        else
            TempData["Error"] = result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    // ── Reject (AJAX) ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        ReviewVerificationDto dto,
        CancellationToken ct)
    {
        dto.Decision = VerificationStatus.Rejected;

        var result = await _verificationService.ReviewAsync(AdminId, dto, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return result.IsSuccess
                ? Json(new { success = true })
                : Json(new
                {
                    success = false,
                    message = result.ErrorMessage
                });

        if (result.IsSuccess)
            TempData["Success"] = "Verification rejected. User has been notified.";
        else
            TempData["Error"] = result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }
}