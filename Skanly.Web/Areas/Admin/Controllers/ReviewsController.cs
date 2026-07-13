// Skanly.Web/Areas/Admin/Controllers/ReviewsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Reviews.Interfaces;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // ── All Reviews ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        string? search = null,
        bool? hiddenOnly = null,
        CancellationToken ct = default)
    {
        var result = await _reviewService.GetAllAsync(
            page, 20, search, hiddenOnly, ct);

        ViewBag.Search = search;
        ViewBag.HiddenOnly = hiddenOnly;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Hide Review (AJAX) ────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id, CancellationToken ct)
    {
        var result = await _reviewService.HideAsync(id, ct);
        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }

    // ── Unhide Review (AJAX) ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unhide(int id, CancellationToken ct)
    {
        var result = await _reviewService.UnhideAsync(id, ct);
        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }

    // ── Hard Delete (AJAX) ────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _reviewService.AdminDeleteAsync(id, ct);
        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }
}