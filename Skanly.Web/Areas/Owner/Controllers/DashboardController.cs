// Skanly.Web/Areas/Owner/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Application.Features.Owners.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class DashboardController : Controller
{
    private readonly IOwnerService _ownerService;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public DashboardController(
        IOwnerService ownerService,
        IUnitOfWork uow,
        INotificationService notificationService)
    {
        _ownerService = ownerService;
        _uow = uow;
        _notificationService = notificationService;
    }

    private string OwnerId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dashResult = await _ownerService
            .GetDashboardAsync(OwnerId, ct);

        if (!dashResult.IsSuccess)
        {
            TempData["Error"] = dashResult.ErrorMessage;
            return View(null);
        }

        var owner = await _uow.Owners.GetByUserIdAsync(OwnerId, ct);

        // Sidebar badges
        ViewBag.OwnerFullName = owner?.FullName;
        ViewBag.OwnerImageUrl = owner?.ProfileImageUrl;
        ViewBag.OwnerIsVerified = owner?.IsIdentityVerified ?? false;
        ViewBag.PropertyCount = dashResult.Data!.TotalProperties;
        ViewBag.PendingRequestsCount = dashResult.Data!.PendingRequests;

        var unreadCount = await _notificationService
            .GetUnreadCountAsync(OwnerId, ct);
        ViewBag.UnreadNotifications = unreadCount.Data;

        var msgCount = await _uow.Chat.GetUnreadCountAsync(OwnerId, ct);
        ViewBag.UnreadMessages = msgCount;

        return View(dashResult.Data);
    }

    [HttpGet]
    public async Task<IActionResult> RecentActivity(CancellationToken ct)
    {
        var bookings = await _uow.Repository<Booking>()
            .GetAllAsync(
            null,
            null,
            ct,
                b => b.Student,
                b => b.Property);

        var ownerPropIds = (await _uow.Properties
            .GetByOwnerIdAsync(OwnerId, false, ct))
            .Select(p => p.Id)
            .ToHashSet();

        var recent = bookings
            .Where(b => ownerPropIds.Contains(b.PropertyId))
            .OrderByDescending(b => b.CreatedAt)
            .Take(6)
            .ToList();

        return PartialView("_RecentActivity", recent);
    }
}