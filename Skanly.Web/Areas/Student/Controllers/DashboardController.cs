// Skanly.Web/Areas/Student/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Students.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class DashboardController : Controller
{
    private readonly IStudentService _studentService;

    public DashboardController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _studentService.GetDashboardAsync(UserId, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return View("Error");
        }

        // Redirect to CompleteProfile if onboarding not done
        if (!result.Data!.IsProfileComplete)
            return RedirectToAction("CompleteProfile", "Profile");

        return View(result.Data);
    }
}