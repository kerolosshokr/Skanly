//Skanly.Web / Areas / Student / Controllers / BookingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Students.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class BookingsController : Controller
{
    private readonly IStudentService _studentService;

    public BookingsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? status = null,
        CancellationToken ct = default)
    {
        var result = await _studentService.GetBookingsAsync(
            UserId, page, pageSize, status, ct);

        ViewBag.StatusFilter = status;
        return result.IsSuccess
            ? View(result.Data)
            : View("Error");
    }
}

