// Skanly.Web/Areas/Student/Controllers/BookingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Bookings.DTOs;
using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Application.Features.Students.Interfaces;
using Skanly.Domain.Enums;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class BookingsController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IPropertyService _propertyService;
    private readonly IStudentService _studentService;

    public BookingsController(
        IBookingService bookingService,
        IPropertyService propertyService,
        IStudentService studentService)
    {
        _bookingService = bookingService;
        _propertyService = propertyService;
        _studentService = studentService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── List ──────────────────────────────────────────────────────────────────

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

    // ── Create (booking request form) ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create(
        int propertyId,
        CancellationToken ct)
    {
        var propertyResult = await _propertyService
            .GetDetailAsync(propertyId, UserId, ct);

        if (!propertyResult.IsSuccess)
        {
            TempData["Error"] = propertyResult.ErrorMessage;
            return RedirectToAction("Search", "Property",
                new { area = "" });
        }

        ViewBag.Property = propertyResult.Data;
        return View(new CreateBookingDto
        {
            PropertyId = propertyId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateBookingDto dto,
        CancellationToken ct)
    {
        var result = await _bookingService.CreateAsync(UserId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            var propertyResult = await _propertyService
                .GetDetailAsync(dto.PropertyId, UserId, ct);

            ViewBag.Property = propertyResult.Data;

            return View(dto);
        }

        TempData["Success"] =
            "Booking request submitted! The owner will respond within 24–48 hours.";

        return RedirectToAction(nameof(Details),
            new { id = result.Data!.BookingId });
    }

    // ── Details ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken ct)
    {
        var result = await _bookingService
            .GetDetailForStudentAsync(UserId, id, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        CancelBookingDto dto,
        CancellationToken ct)
    {
        var result = await _bookingService.CancelAsync(UserId, dto, ct);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? "Booking cancelled successfully."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Details),
            new { id = dto.BookingId });
    }

    // ── Proceed to payment ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProceedToPayment(
        int bookingId,
        CancellationToken ct)
    {
        var result = await _bookingService
            .MarkPaymentPendingAsync(UserId, bookingId, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Details),
                new { id = bookingId });
        }

        return RedirectToAction(
            "Checkout",
            "Payment",
            new
            {
                area = "",
                bookingId
            });
    }
}