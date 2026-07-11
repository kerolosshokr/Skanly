// Skanly.Web/Areas/Student/Controllers/FavoritesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Students.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class FavoritesController : Controller
{
    private readonly IStudentService _studentService;

    public FavoritesController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _studentService.GetFavoritesAsync(UserId, page, 12, ct);
        return result.IsSuccess ? View(result.Data) : View("Error");
    }
}