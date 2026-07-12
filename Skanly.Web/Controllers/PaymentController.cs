// Skanly.Web/Controllers/PaymentController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Payments.DTOs;
using Skanly.Application.Features.Payments.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[Authorize(Policy = "StudentOnly")]
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private string StudentId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Checkout Page ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Checkout(int bookingId, CancellationToken ct)
    {
        var result = await _paymentService.GetCheckoutAsync(StudentId, bookingId, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Bookings", new { area = "Student" });
        }

        return View(result.Data);
    }

    // ── Process Payment ───────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(
        InitiatePaymentDto dto,
        CancellationToken ct)
    {
        var result = await _paymentService
            .ProcessPaymentAsync(StudentId, dto, ct);

        if (!result.IsSuccess)
        {
            // Validation or service-level error (not gateway failure)
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            // Reload checkout view
            var checkoutResult = await _paymentService
                .GetCheckoutAsync(StudentId, dto.BookingId, ct);

            if (checkoutResult.IsSuccess)
            {
                checkoutResult.Data!.PaymentForm.PaymentMethod = dto.PaymentMethod;
                return View("Checkout", checkoutResult.Data);
            }

            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Bookings", new { area = "Student" });
        }

        // Gateway returned a result (success OR failure)
        var paymentResult = result.Data!;

        if (paymentResult.IsSuccess)
        {
            return RedirectToAction(nameof(Success), new
            {
                paymentId = paymentResult.PaymentId,
                bookingId = paymentResult.BookingId,
                txRef = paymentResult.TransactionReference,
                amount = paymentResult.AmountPaid
            });
        }
        else
        {
            return RedirectToAction(nameof(Failed), new
            {
                bookingId = paymentResult.BookingId,
                reason = paymentResult.FailureReason
            });
        }
    }

    // ── Success Page ──────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Success(
        int paymentId,
        int bookingId,
        string txRef,
        decimal amount)
    {
        ViewBag.PaymentId = paymentId;
        ViewBag.BookingId = bookingId;
        ViewBag.TxRef = txRef;
        ViewBag.Amount = amount;
        return View();
    }

    // ── Failed Page ───────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Failed(int bookingId, string? reason)
    {
        ViewBag.BookingId = bookingId;
        ViewBag.Reason = reason ?? "An unexpected error occurred.";
        return View();
    }

    // ── Payment History ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> History(
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _paymentService
            .GetStudentPaymentHistoryAsync(StudentId, page, 10, ct);

        return result.IsSuccess ? View(result.Data) : View("Error");
    }
}