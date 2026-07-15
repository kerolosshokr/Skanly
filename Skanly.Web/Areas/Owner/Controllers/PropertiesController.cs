// Skanly.Web/Areas/Owner/Controllers/PropertiesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Owners.Interfaces;
using OwnerEntity = Skanly.Domain.Entities.Owner;
using Skanly.Domain.Entities;
using Skanly.Application.Features.Properties.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class PropertiesController : Controller
{
    private readonly IPropertyService _propertyService;
    private readonly IOwnerService _ownerService;
    private readonly IUnitOfWork _uow;

    public PropertiesController(
        IPropertyService propertyService,
        IOwnerService ownerService,
        IUnitOfWork uow)
    {
        _propertyService = propertyService;
        _ownerService = ownerService;
        _uow = uow;
    }

    private string OwnerId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Index ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        string? status = null,
        CancellationToken ct = default)
    {
        var result = await _ownerService.GetPropertiesAsync(
            OwnerId, page, 12, status, ct);

        var owner = await _uow.Owners.GetByUserIdAsync(OwnerId, ct);
        SetSidebarBadges(owner);
        ViewBag.StatusFilter = status;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Toggle Availability (AJAX) ────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability(
        int propertyId,
        CancellationToken ct)
    {
        var property = await _uow.Properties
            .GetByIdAsync(propertyId, ct);

        if (property is null || property.OwnerId != OwnerId)
            return Json(new
            {
                success = false,
                message = "Property not found."
            });

        property.IsAvailable = !property.IsAvailable;
        _uow.Repository<Property>().Update(property);
        await _uow.SaveChangesAsync(ct);

        return Json(new
        {
            success = true,
            isAvailable = property.IsAvailable,
            message = property.IsAvailable
                ? "Property is now available."
                : "Property marked as unavailable."
        });
    }

    // ── Delete (AJAX) ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int propertyId,
        CancellationToken ct)
    {
        var result = await _propertyService.SoftDeleteAsync(OwnerId, propertyId, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new
            {
                success = false,
                message = result.ErrorMessage
            });
    }

    private void SetSidebarBadges(OwnerEntity? owner)
    {
        ViewBag.OwnerFullName = owner?.FullName;
        ViewBag.OwnerImageUrl = owner?.ProfileImageUrl;
        ViewBag.OwnerIsVerified = owner?.IsIdentityVerified ?? false;
    }
}