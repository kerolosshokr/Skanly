// Skanly.Web/Areas/Owner/Controllers/BookingRequestsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Owners.DTOs;
using Skanly.Application.Features.Owners.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class BookingRequestsController : Controller
{
    private readonly IOwnerService _ownerService;

    public BookingRequestsController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        string? status = null,
        CancellationToken ct = default)
    {
        var result = await _ownerService
            .GetBookingRequestsAsync(UserId, page, 10, status, ct);

        ViewBag.StatusFilter = status;
        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await _ownerService
            .GetBookingRequestDetailAsync(UserId, id, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Handle(
        HandleBookingRequestDto dto,
        CancellationToken ct)
    {
        var result = await _ownerService.HandleBookingRequestAsync(UserId, dto, ct);

        if (!result.IsSuccess)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new
        {
            success = true,
            message = dto.Accept
                ? "Booking accepted. Student has been notified to proceed with payment."
                : "Booking rejected. Student has been notified."
        });
    }
}