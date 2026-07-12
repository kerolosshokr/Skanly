// Skanly.Web/Areas/Admin/Controllers/BookingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class BookingsController : Controller
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        BookingStatus? status = null,
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _bookingService.GetAllAsync(
            page, 20, status, search, from, to, ct);

        var stats = await _bookingService.GetStatsAsync(ct);

        ViewBag.StatusFilter = status;
        ViewBag.Search = search;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.Stats = stats.Data;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken ct)
    {
        var result = await _bookingService.GetDetailForAdminAsync(id, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }
}