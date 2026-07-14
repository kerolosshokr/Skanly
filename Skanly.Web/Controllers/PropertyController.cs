// Skanly.Web/Controllers/PropertyController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Maps.Interfaces;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Application.Features.Recommendations.DTOs;
using Skanly.Application.Features.Recommendations.Interfaces;
using Skanly.Application.Features.Universities.Interfaces;
using Skanly.Domain.Entities;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[AllowAnonymous]
public class PropertyController : Controller
{
    private readonly IPropertyService _propertyService;
    private readonly IUniversityService _universityService;
    private readonly IUnitOfWork _uow;
    private readonly IGoogleMapsService _mapsService;
    private readonly IRecommendationService _recommendationService;

    public PropertyController(
        IPropertyService propertyService,
        IUniversityService universityService,
        IUnitOfWork uow,
        IGoogleMapsService mapsService,
        IRecommendationService recommendationService)
    {
        _propertyService = propertyService;
        _universityService = universityService;
        _uow = uow;
        _mapsService = mapsService;
        _recommendationService = recommendationService;
    }

    private string? ViewerUserId =>
        User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    // ─────────────────────────────────────────────────────────────
    // Search Page
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] PropertySearchRequestDto request,
        CancellationToken ct)
    {
        var result = await _propertyService.SearchAsync(request, ViewerUserId, ct);

        // Record search for recommendation engine
        if (User.Identity?.IsAuthenticated == true &&
            User.IsInRole("Student"))
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(studentId))
            {
                _ = _recommendationService.RecordSearchAsync(
                    studentId,
                    new StudentSearchHistoryEntry
                    {
                        UniversityId = request.UniversityId,
                        AreaId = request.AreaId,
                        MinPrice = request.MinPrice,
                        MaxPrice = request.MaxPrice,
                        PropertyType = request.PropertyType,
                        SearchedAt = DateTime.UtcNow
                    },
                    CancellationToken.None);
            }
        }

        await LoadSearchDropdownsAsync(ct);

        ViewBag.Request = request;

        return View(result.Data);
    }

    // ─────────────────────────────────────────────────────────────
    // AJAX Search
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> SearchPartial(
        [FromQuery] PropertySearchRequestDto request,
        CancellationToken ct)
    {
        var result = await _propertyService.SearchAsync(request, ViewerUserId, ct);
        return PartialView("_PropertyGrid", result.Data);
    }

    // ─────────────────────────────────────────────────────────────
    // Property Details
    // ─────────────────────────────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await _propertyService.GetDetailAsync(id, ViewerUserId, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Search));
        }

        var property = result.Data;

        if (property is null)
        {
            return NotFound();
        }

        var related = await _propertyService.GetRelatedAsync(id, 4, ct);
        ViewBag.RelatedProperties = related.Data;

        var apiKey = _mapsService.GetBrowserApiKey();

        decimal uniLat = 0;
        decimal uniLng = 0;

        if (property.UniversityId.HasValue)
        {
            var uni = await _uow.Universities.GetByIdAsync(
                property.UniversityId.Value,
                ct);

            if (uni != null)
            {
                uniLat = uni.Latitude;
                uniLng = uni.Longitude;
            }
        }

        ViewData["BrowserApiKey"] = apiKey;
        ViewData["UniversityLat"] = uniLat;
        ViewData["UniversityLng"] = uniLng;

        return View(result.Data);
    }

    // ─────────────────────────────────────────────────────────────
    // Amenities API
    // ─────────────────────────────────────────────────────────────

    [HttpGet("api/amenities")]
    public async Task<IActionResult> GetAmenities(CancellationToken ct)
    {
        var result = await _propertyService.GetAllAmenitiesAsync(ct);
        return Ok(result.Data);
    }

    // ─────────────────────────────────────────────────────────────
    // Areas API
    // ─────────────────────────────────────────────────────────────

    [HttpGet("api/areas")]
    public async Task<IActionResult> GetAreas(CancellationToken ct)
    {
        var areas = await _uow.Repository<Area>().GetAllAsync(ct);

        return Ok(areas.Select(a => new
        {
            a.Id,
            a.NameEn,
            a.NameAr
        }));
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

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