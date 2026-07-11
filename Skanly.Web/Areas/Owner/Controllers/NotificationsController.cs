using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Owners.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class NotificationsController : Controller
{
    private readonly IOwnerService _ownerService;

    public NotificationsController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _ownerService
            .GetNotificationsAsync(UserId, page, 20, ct);
        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await _ownerService.MarkNotificationReadAsync(UserId, id, ct);
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _ownerService.MarkAllNotificationsReadAsync(UserId, ct);
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var result = await _ownerService
            .GetUnreadNotificationCountAsync(UserId, ct);
        return Ok(new { count = result.Data });
    }
}
