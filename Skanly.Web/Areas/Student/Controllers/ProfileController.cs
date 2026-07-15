// Skanly.Web/Areas/Student/Controllers/ProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Students.DTOs;
using Skanly.Application.Features.Students.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class ProfileController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IUnitOfWork _uow;

    public ProfileController(
        IStudentService studentService,
        IUnitOfWork uow)
    {
        _studentService = studentService;
        _uow = uow;
    }

    private string StudentId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _studentService.GetProfileAsync(StudentId, ct);
        if (!result.IsSuccess)
            return View("Error");

        var profile = result.Data!;
        SetSidebarBadges(profile);

        return View(profile);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        UpdateProfileDto dto,
        CancellationToken ct)
    {
        var result = await _studentService
            .UpdateProfileAsync(StudentId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            var profileResult = await _studentService
                .GetProfileAsync(StudentId, ct);
            SetSidebarBadges(profileResult.Data!);
            return View("Index", profileResult.Data);
        }

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(
        IFormFile image,
        CancellationToken ct)
    {
        var result = await _studentService
            .UploadProfileImageAsync(StudentId, image, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Profile photo updated.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void SetSidebarBadges(StudentProfileDto? profile)
    {
        if (profile is null) return;
        ViewBag.StudentFullName = profile.FullName;
        ViewBag.StudentImageUrl = profile.ProfileImageUrl;
        ViewBag.StudentIsVerified = profile.IsIdentityVerified;
        ViewBag.UniversityName = profile.UniversityNameEn;
    }
}