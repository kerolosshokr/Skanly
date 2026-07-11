// Skanly.Web/Areas/Student/Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Students.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class NotificationsController : Controller
{
    private readonly IStudentService _studentService;

    public NotificationsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _studentService
            .GetNotificationsAsync(UserId, page, 20, ct);
        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await _studentService.MarkNotificationReadAsync(UserId, id, ct);
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _studentService.MarkAllNotificationsReadAsync(UserId, ct);
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var result = await _studentService
            .GetUnreadNotificationCountAsync(UserId, ct);
        return Ok(new { count = result.Data });
    }
}