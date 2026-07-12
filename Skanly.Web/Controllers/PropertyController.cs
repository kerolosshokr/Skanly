// Skanly.Web/Controllers/PropertyController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Application.Features.Universities.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Application.Common.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[AllowAnonymous]
public class PropertyController : Controller
{
    private readonly IPropertyService _propertyService;
    private readonly IUniversityService _universityService;
    private readonly IUnitOfWork _uow;

    public PropertyController(
        IPropertyService propertyService,
        IUniversityService universityService,
        IUnitOfWork uow)
    {
        _propertyService = propertyService;
        _universityService = universityService;
        _uow = uow;
    }

    private string? ViewerUserId =>
        User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    // ── Search Page ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] PropertySearchRequestDto request,
        CancellationToken ct)
    {
        var result = await _propertyService.SearchAsync(request, ViewerUserId, ct);
        await LoadSearchDropdownsAsync(ct);
        ViewBag.Request = request;
        return View(result.Data);
    }

    // ── AJAX Search (for live filter update) ──────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> SearchPartial(
        [FromQuery] PropertySearchRequestDto request,
        CancellationToken ct)
    {
        var result = await _propertyService.SearchAsync(request, ViewerUserId, ct);
        return PartialView("_PropertyGrid", result.Data);
    }

    // ── Property Detail ────────────────────────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await _propertyService.GetDetailAsync(id, ViewerUserId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Search));
        }

        // Load related properties for sidebar
        var related = await _propertyService.GetRelatedAsync(id, 4, ct);
        ViewBag.RelatedProperties = related.Data;

        return View(result.Data);
    }

    // ── Amenities API (for dynamic filter panel) ──────────────────────────────

    [HttpGet("api/amenities")]
    public async Task<IActionResult> GetAmenities(CancellationToken ct)
    {
        var result = await _propertyService.GetAllAmenitiesAsync(ct);
        return Ok(result.Data);
    }

    // ── Areas API ─────────────────────────────────────────────────────────────

    [HttpGet("api/areas")]
    public async Task<IActionResult> GetAreas(CancellationToken ct)
    {
        var areas = await _uow.Repository<Area>().GetAllAsync(ct);
        return Ok(areas.Select(a => new { a.Id, a.NameEn, a.NameAr }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task LoadSearchDropdownsAsync(CancellationToken ct)
    {
        var universities = await _universityService.GetActiveListAsync(ct);
        var amenities = await _propertyService.GetAllAmenitiesAsync(ct);
        var areas = await _uow.Repository<Area>().GetAllAsync(ct);

        ViewBag.Universities = universities.Data;
        ViewBag.Amenities = amenities.Data;
        ViewBag.Areas = areas;
    }
}