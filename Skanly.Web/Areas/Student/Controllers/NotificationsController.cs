// Skanly.Web/Controllers/NotificationsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Application.Features.Students.Interfaces;
using Skanly.Domain.Enums;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IStudentService _studentService;

    public NotificationsController(
        INotificationService notificationService,
        IStudentService studentService)
    {
        _notificationService = notificationService;
        _studentService = studentService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ─────────────────────────────────────────────────────────────
    // Full Notifications Page
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        NotificationType? type = null,
        bool? unreadOnly = null,
        CancellationToken ct = default)
    {
        var result = await _notificationService.GetPagedAsync(
            UserId,
            page,
            20,
            type,
            unreadOnly,
            ct);

        if (!result.IsSuccess)
            return View("Error");

        var countResult =
            await _notificationService.GetUnreadCountAsync(UserId, ct);

        ViewBag.TypeFilter = type;
        ViewBag.UnreadOnly = unreadOnly;
        ViewBag.UnreadCount = countResult.Data;

        return View(result.Data);
    }

    // ─────────────────────────────────────────────────────────────
    // Recent Notifications (Navbar AJAX)
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Recent(CancellationToken ct)
    {
        var result =
            await _notificationService.GetRecentAsync(UserId, 8, ct);

        var count =
            await _notificationService.GetUnreadCountAsync(UserId, ct);

        return Ok(new
        {
            notifications = result.Data,
            unreadCount = count.Data
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Unread Count (AJAX)
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var result =
            await _notificationService.GetUnreadCountAsync(UserId, ct);

        return Ok(new
        {
            count = result.Data
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Mark One Notification Read
    // ─────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        var result =
            await _notificationService.MarkReadAsync(UserId, id, ct);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        var count =
            await _notificationService.GetUnreadCountAsync(UserId, ct);

        return Ok(new
        {
            unreadCount = count.Data
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Mark All Notifications Read
    // ─────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notificationService.MarkAllReadAsync(UserId, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok(new
            {
                unreadCount = 0
            });
        }

        TempData["Success"] = "All notifications marked as read.";

        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────────────────────
    // Student Notifications (Compatibility)
    // ─────────────────────────────────────────────────────────────

    [Authorize(Policy = "StudentOnly")]
    [HttpGet]
    public async Task<IActionResult> StudentIndex(
        int page = 1,
        CancellationToken ct = default)
    {
        var result =
            await _studentService.GetNotificationsAsync(UserId, page, 20, ct);

        return result.IsSuccess
            ? View("Index", result.Data)
            : View("Error");
    }

    [Authorize(Policy = "StudentOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentMarkRead(
        long id,
        CancellationToken ct)
    {
        await _studentService.MarkNotificationReadAsync(UserId, id, ct);

        var count =
            await _studentService.GetUnreadNotificationCountAsync(UserId, ct);

        return Ok(new
        {
            unreadCount = count.Data
        });
    }

    [Authorize(Policy = "StudentOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentMarkAllRead(
        CancellationToken ct)
    {
        await _studentService.MarkAllNotificationsReadAsync(UserId, ct);

        TempData["Success"] = "All notifications marked as read.";

        return RedirectToAction(nameof(StudentIndex));
    }

    [Authorize(Policy = "StudentOnly")]
    [HttpGet]
    public async Task<IActionResult> StudentUnreadCount(
        CancellationToken ct)
    {
        var result =
            await _studentService.GetUnreadNotificationCountAsync(UserId, ct);

        return Ok(new
        {
            count = result.Data
        });
    }
}