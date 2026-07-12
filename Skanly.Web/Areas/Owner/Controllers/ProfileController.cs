// Skanly.Web/Areas/Owner/Controllers/ProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Owners.DTOs;
using Skanly.Application.Features.Owners.Interfaces;
using Skanly.Application.Features.Students.DTOs;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class ProfileController : Controller
{

    private readonly IOwnerService _ownerService;

    public ProfileController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _ownerService.GetProfileAsync(UserId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Dashboard");
        }
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var result = await _ownerService.GetProfileAsync(UserId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateOwnerProfileDto
        {
            FirstName = result.Data!.FirstName,
            LastName = result.Data.LastName,
            PhoneNumber = result.Data.PhoneNumber,
            BusinessName = result.Data.BusinessName
        };
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UpdateOwnerProfileDto dto,
        CancellationToken ct)
    {
        var result = await _ownerService.UpdateProfileAsync(UserId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(dto);
        }

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(
        IFormFile image,
        CancellationToken ct)
    {
        var result = await _ownerService.UploadProfileImageAsync(UserId, image, ct);
        return result.IsSuccess
            ? Json(new { success = true, imageUrl = result.Data })
            : Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpGet]
    public async Task<IActionResult> VerifyIdentity(CancellationToken ct)
    {
        var profileResult = await _ownerService.GetProfileAsync(UserId, ct);
        ViewBag.VerificationStatus = profileResult.Data?.VerificationStatus ?? "Not Submitted";
        return View(new UploadIdentityDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyIdentity(
        UploadIdentityDto dto,
        CancellationToken ct)
    {
        var result = await _ownerService.SubmitIdentityVerificationAsync(UserId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            ViewBag.VerificationStatus = "Not Submitted";
            return View(dto);
        }

        TempData["Success"] =
            "Documents submitted. Admin will review within 24–48 hours.";
        return RedirectToAction(nameof(Index));
    }
}