// Skanly.Web/Areas/Student/Controllers/ProfileController.cs
using Skanly.Application.Features.Universities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Students.DTOs;
using Skanly.Application.Features.Students.Interfaces;
using Skanly.Application.Features.Universities.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class ProfileController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IUniversityService _universityService;

    public ProfileController(
        IStudentService studentService,
        IUniversityService universityService)
    {
        _studentService = studentService;
        _universityService = universityService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── View Profile ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _studentService.GetProfileAsync(UserId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Dashboard");
        }
        return View(result.Data);
    }

    // ── Edit Profile ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var result = await _studentService.GetProfileAsync(UserId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        await LoadUniversitiesAsync(ct);

        var dto = new UpdateProfileDto
        {
            FirstName = result.Data!.FirstName,
            LastName = result.Data.LastName,
            PhoneNumber = result.Data.PhoneNumber,
            BirthDate = result.Data.BirthDate,
            UniversityId = result.Data.UniversityId
        };

        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UpdateProfileDto dto,
        CancellationToken ct)
    {
        var result = await _studentService.UpdateProfileAsync(UserId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            await LoadUniversitiesAsync(ct);
            return View(dto);
        }

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Complete Profile (Onboarding) ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> CompleteProfile(CancellationToken ct)
    {
        await LoadUniversitiesAsync(ct);
        return View(new CompleteProfileDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(
        CompleteProfileDto dto,
        CancellationToken ct)
    {
        var result = await _studentService.CompleteProfileAsync(UserId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            await LoadUniversitiesAsync(ct);
            return View(dto);
        }

        TempData["Success"] = "Profile completed! Welcome to Skanly.";
        return RedirectToAction("Index", "Dashboard");
    }

    // ── Upload Profile Image (AJAX) ────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(
        IFormFile image,
        CancellationToken ct)
    {
        var result = await _studentService.UploadProfileImageAsync(UserId, image, ct);

        return result.IsSuccess
            ? Json(new { success = true, imageUrl = result.Data })
            : Json(new { success = false, message = result.ErrorMessage });
    }

    // ── Identity Verification ─────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> VerifyIdentity(CancellationToken ct)
    {
        var statusResult = await _studentService
            .GetVerificationStatusAsync(UserId, ct);

        ViewBag.VerificationStatus = statusResult.Data ?? "Not Submitted";
        return View(new UploadIdentityDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyIdentity(
        UploadIdentityDto dto,
        CancellationToken ct)
    {
        var result = await _studentService
            .SubmitIdentityVerificationAsync(UserId, dto, ct);

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
            "Documents submitted successfully. Admin will review within 24–48 hours.";
        return RedirectToAction(nameof(Index));
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task LoadUniversitiesAsync(CancellationToken ct)
    {
        var result = await _universityService.GetActiveListAsync(ct);

        ViewBag.Universities = result.Data ?? Array.Empty<UniversityDto>();
    }
}