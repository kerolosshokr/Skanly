// Skanly.Web/Areas/Owner/Controllers/PropertiesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Application.Features.Universities.Interfaces;
using Skanly.Application.Common.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Application.Features.Owners.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class PropertiesController : Controller
{
    private readonly IPropertyService _propertyService;
    private readonly IUniversityService _universityService;
    private readonly IUnitOfWork _uow;
    private readonly IOwnerService _ownerService;

    public PropertiesController(
    IPropertyService propertyService,
    IUniversityService universityService,
    IUnitOfWork uow,
    IOwnerService ownerService)
    {
        _propertyService = propertyService;
        _universityService = universityService;
        _uow = uow;
        _ownerService = ownerService;
    }


    private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    // ── Create ────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadFormDropdownsAsync(ct);
        return View(new CreatePropertyDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB total
    public async Task<IActionResult> Create(
        CreatePropertyDto dto,
        CancellationToken ct)
    {
        var result = await _propertyService.CreateAsync(OwnerId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            await LoadFormDropdownsAsync(ct);
            return View(dto);
        }

        TempData["Success"] =
            "Property submitted successfully! It will be reviewed by our team " +
            "within 24–48 hours before going live.";

        return RedirectToAction(
            nameof(Index),
            new { area = "Owner" });
    }
    // ── Edit ──────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _propertyService.GetDetailAsync(id, OwnerId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var p = result.Data!;
        if (p.OwnerId != OwnerId)
        {
            TempData["Error"] = "Access denied.";
            return RedirectToAction(nameof(Index));
        }

        await LoadFormDropdownsAsync(ct);

        var dto = new UpdatePropertyDto
        {
            PropertyId = p.PropertyId,
            Title = p.Title,
            Description = p.Description,
            PropertyType = p.PropertyType,
            SmokingAllowed = p.SmokingAllowed,
            Rooms = p.Rooms,
            Beds = p.Beds,
            PricePerMonth = p.PricePerMonth,
            Address = p.Address,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            UniversityId = p.UniversityId,
            AreaId = p.AreaId,
            IsAvailable = p.IsAvailable,
            AmenityIds = p.Amenities.Select(a => a.AmenityId).ToList()
        };

        ViewBag.ExistingImages = p.Images;
        ViewBag.ExistingVideos = p.VideoUrls;
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        UpdatePropertyDto dto,
        CancellationToken ct)
    {
        var result = await _propertyService.UpdateAsync(OwnerId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            await LoadFormDropdownsAsync(ct);
            var detail = await _propertyService.GetDetailAsync(dto.PropertyId, OwnerId, ct);
            ViewBag.ExistingImages = detail.Data?.Images;
            ViewBag.ExistingVideos = detail.Data?.VideoUrls;
            return View(dto);
        }

        TempData["Success"] =
            "Property updated. It has been sent back for re-approval.";
        return RedirectToAction(nameof(Index));
    }
    // ── Delete (AJAX soft delete) ─────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _propertyService.SoftDeleteAsync(OwnerId, id, ct);
        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }
    // ── Toggle Availability (AJAX) ────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability(int id, CancellationToken ct)
    {
        var result = await _propertyService.ToggleAvailabilityAsync(OwnerId, id, ct);
        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }
    // ── Set Primary Image (AJAX) ──────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryImage(
        int propertyId,
        int imageId,
        CancellationToken ct)
    {
        var result = await _propertyService
            .SetPrimaryImageAsync(OwnerId, propertyId, imageId, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }
    // ── Delete Image (AJAX) ───────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(
       int propertyId,
       int imageId,
       CancellationToken ct)
    {
        var result = await _propertyService
            .DeleteImageAsync(OwnerId, propertyId, imageId, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }
    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task LoadFormDropdownsAsync(CancellationToken ct)
    {
        var universities = await _universityService.GetActiveListAsync(ct);
        var amenities = await _propertyService.GetAllAmenitiesAsync(ct);
        var areas = await _uow.Repository<Area>().GetAllAsync(ct);

        ViewBag.Universities = universities.Data;
        ViewBag.Amenities = amenities.Data;
        ViewBag.Areas = areas;
    }
  


 
    //public PropertiesController(IOwnerService ownerService)
    //{
    //    _ownerService = ownerService;
    //}

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? status = null,
        CancellationToken ct = default)
    {
        var result = await _ownerService
            .GetPropertiesAsync(UserId, page, pageSize, status, ct);

        ViewBag.StatusFilter = status;
        ViewBag.PageSize = pageSize;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }
}