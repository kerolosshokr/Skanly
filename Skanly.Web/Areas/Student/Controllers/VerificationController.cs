// Skanly.Web/Areas/Student/Controllers/VerificationController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Verification.DTOs;
using Skanly.Application.Features.Verification.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class VerificationController : Controller
{
    private readonly IVerificationService _verificationService;

    public VerificationController(IVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Status Page ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _verificationService
            .GetLatestForUserAsync(UserId, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Dashboard");
        }

        return View(result.Data);
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Submit(CancellationToken ct)
    {
        // Redirect to status if already submitted
        var latest = await _verificationService
            .GetLatestForUserAsync(UserId, ct);

        if (latest.IsSuccess && latest.Data is not null)
            return RedirectToAction(nameof(Index));

        return View(new SubmitVerificationDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Submit(
        SubmitVerificationDto dto,
        CancellationToken ct)
    {
        var result = await _verificationService.SubmitAsync(UserId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            return View(dto);
        }

        TempData["Success"] =
            "Documents submitted successfully! " +
            "Our team will review your submission within 24–48 hours.";

        return RedirectToAction(nameof(Index));
    }

    // ── History ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        var result = await _verificationService
            .GetHistoryAsync(UserId, ct);

        return result.IsSuccess
            ? View(result.Data)
            : View("Error");
    }
}