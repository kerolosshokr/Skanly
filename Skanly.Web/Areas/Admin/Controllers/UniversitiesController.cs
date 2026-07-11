// Skanly.Web/Areas/Admin/Controllers/UniversitiesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Universities.DTOs;
using Skanly.Application.Features.Universities.Interfaces;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UniversitiesController : Controller
{
    private readonly IUniversityService _universityService;

    public UniversitiesController(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await _universityService.GetAllAsync(
            page, pageSize, search, isActive, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(PagedResult<UniversityDto>.Empty());
        }

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        ViewBag.PageSize = pageSize;

        return View(result.Data);
    }

    // ── Details ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await _universityService.GetByIdAsync(id, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Create()
        => View(new CreateUniversityDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateUniversityDto dto,
        CancellationToken ct)
    {
        var result = await _universityService.CreateAsync(dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any() && result.ErrorMessage is not null)
                ModelState.AddModelError(string.Empty, result.ErrorMessage);

            return View(dto);
        }

        TempData["Success"] = $"University '{result.Data!.NameEn}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _universityService.GetByIdAsync(id, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateUniversityDto
        {
            UniversityId = result.Data!.UniversityId,
            NameAr = result.Data.NameAr,
            NameEn = result.Data.NameEn,
            Address = result.Data.Address,
            Latitude = result.Data.Latitude,
            Longitude = result.Data.Longitude,
            Description = result.Data.Description,
            IsActive = result.Data.IsActive
        };

        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UpdateUniversityDto dto,
        CancellationToken ct)
    {
        var result = await _universityService.UpdateAsync(dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any() && result.ErrorMessage is not null)
                ModelState.AddModelError(string.Empty, result.ErrorMessage);

            return View(dto);
        }

        TempData["Success"] = $"University '{result.Data!.NameEn}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Toggle Active (AJAX) ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var result = await _universityService.ToggleActiveAsync(id, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _universityService.DeleteAsync(id, ct);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? "University deleted successfully."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    // ── API endpoint for dropdowns (used by student registration, property forms) ──

    [AllowAnonymous]
    [HttpGet("api/universities/active")]
    public async Task<IActionResult> GetActiveList(CancellationToken ct)
    {
        var result = await _universityService.GetActiveListAsync(ct);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }
}