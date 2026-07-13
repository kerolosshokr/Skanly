// Skanly.Web/Areas/Student/Controllers/FavoritesController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Favorites.Interfaces;
using Skanly.Application.Features.Students.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class FavoritesController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(
        IStudentService studentService,
        IFavoriteService favoriteService)
    {
        _studentService = studentService;
        _favoriteService = favoriteService;
    }

    private string StudentId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ───────────────────────────────────────────────────────────────
    // Favorites List
    // ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 12,
        string? sortBy = "Newest",
        string? search = null,
        CancellationToken ct = default)
    {
        // استخدام StudentService لجلب المفضلة
        var result = await _studentService.GetFavoritesAsync(
            StudentId,
            page,
            pageSize,
            ct);

        // استخدام FavoriteService لجلب العدد
        var countResult = await _favoriteService.GetCountAsync(StudentId, ct);

        ViewBag.SortBy = sortBy;
        ViewBag.Search = search;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = countResult.Data;

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(PagedResult<object>.Empty());
        }

        return View(result.Data);
    }

    // ───────────────────────────────────────────────────────────────
    // Toggle Favorite (AJAX)
    // ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(
        int propertyId,
        CancellationToken ct)
    {
        var result = await _favoriteService.ToggleAsync(
            StudentId,
            propertyId,
            ct);

        if (!result.IsSuccess)
        {
            return Json(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        return Json(new
        {
            success = true,
            isFavorited = result.Data!.IsFavorited,
            totalFavorites = result.Data.TotalFavorites,
            message = result.Data.Message
        });
    }

    // ───────────────────────────────────────────────────────────────
    // Remove Favorite
    // ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        int propertyId,
        string? returnUrl,
        CancellationToken ct)
    {
        var result = await _favoriteService.RemoveAsync(
            StudentId,
            propertyId,
            ct);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? "Property removed from saved homes."
                : result.ErrorMessage;

        if (!string.IsNullOrEmpty(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // ───────────────────────────────────────────────────────────────
    // Check Favorite
    // ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Check(
        int propertyId,
        CancellationToken ct)
    {
        var result = await _favoriteService.IsFavoritedAsync(
            StudentId,
            propertyId,
            ct);

        return Ok(new
        {
            isFavorited = result.Data
        });
    }

    // ───────────────────────────────────────────────────────────────
    // Get Favorite Property IDs
    // ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetIds(
        CancellationToken ct)
    {
        var result = await _favoriteService
            .GetFavoritedPropertyIdsAsync(StudentId, ct);

        return Ok(new
        {
            ids = result.Data
        });
    }

    // ───────────────────────────────────────────────────────────────
    // Clear All Favorites
    // ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearAll(
        CancellationToken ct)
    {
        var result = await _favoriteService.ClearAllAsync(
            StudentId,
            ct);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? "All saved homes have been cleared."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    // ───────────────────────────────────────────────────────────────
    // Favorites Count
    // ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Count(
        CancellationToken ct)
    {
        var result = await _favoriteService.GetCountAsync(
            StudentId,
            ct);

        return Ok(new
        {
            count = result.Data
        });
    }
}