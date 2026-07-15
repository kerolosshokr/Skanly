// Skanly.Web/Areas/Owner/Controllers/BookingRequestsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Application.Features.Owners.Interfaces;
using Skanly.Domain.Enums;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class BookingRequestsController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IOwnerService _ownerService;
    private readonly IUnitOfWork _uow;

    public BookingRequestsController(
        IBookingService bookingService,
        IOwnerService ownerService,
        IUnitOfWork uow)
    {
        _bookingService = bookingService;
        _ownerService = ownerService;
        _uow = uow;
    }

    private string OwnerId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        BookingStatus? status = null,
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _bookingService.GetByOwnerAsync(
            OwnerId, page, 15, status, ct);

        var pending = await _uow.Repository<Skanly.Domain.Entities.Booking>()
            .CountAsync(b =>
                b.Property.OwnerId == OwnerId &&
                b.Status == BookingStatus.Pending, ct);

        var owner = await _uow.Owners.GetByUserIdAsync(OwnerId, ct);
        ViewBag.OwnerFullName = owner?.FullName;
        ViewBag.OwnerImageUrl = owner?.ProfileImageUrl;
        ViewBag.OwnerIsVerified = owner?.IsIdentityVerified ?? false;
        ViewBag.PendingRequestsCount = pending;
        ViewBag.StatusFilter = status;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken ct)
    {
        var result = await _bookingService
            .GetDetailForOwnerAsync(OwnerId, id, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── Accept (AJAX) ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(
        int bookingId,
        CancellationToken ct)
    {
        var result = await _bookingService.AcceptAsync(
            OwnerId, bookingId, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new
            {
                success = false,
                message = result.ErrorMessage
            });
    }

    // ── Reject (AJAX) ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int bookingId,
        string reason,
        CancellationToken ct)
    {
        var result = await _bookingService.RejectAsync(
            OwnerId, bookingId, reason, ct);

        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new
            {
                success = false,
                message = result.ErrorMessage
            });
    }
}