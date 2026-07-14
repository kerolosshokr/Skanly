// Skanly.Web/Controllers/ContractController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Contracts.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[Authorize]
public class ContractController : Controller
{
    private readonly IPdfContractService _contractService;

    public ContractController(IPdfContractService contractService)
    {
        _contractService = contractService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin =>
        User.IsInRole("Admin");

    // ── Download contract PDF ─────────────────────────────────────────────────

    [HttpGet("/Contract/Download/{bookingId:int}")]
    public async Task<IActionResult> Download(
        int bookingId,
        CancellationToken ct)
    {
        var result = await _contractService.GetPdfBytesAsync(
            UserId, bookingId, IsAdmin, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return NotFound(result.ErrorMessage);
        }

        // Get contract number for filename
        var contractResult = await _contractService.GetByBookingAsync(
            UserId, bookingId, IsAdmin, ct);

        var contractNumber = contractResult.Data?.ContractNumber
                             ?? $"Contract-{bookingId}";
        var fileName = $"{contractNumber}.pdf";

        return File(result.Data!,
            "application/pdf",
            fileName,
            enableRangeProcessing: false);
    }

    // ── View contract inline (browser renders the PDF) ────────────────────────

    [HttpGet("/Contract/View/{bookingId:int}")]
    public async Task<IActionResult> View(
        int bookingId,
        CancellationToken ct)
    {
        var result = await _contractService.GetPdfBytesAsync(
            UserId, bookingId, IsAdmin, ct);

        if (!result.IsSuccess)
            return NotFound(result.ErrorMessage);

        // inline disposition → browser opens the PDF rather than downloading
        Response.Headers["Content-Disposition"] =
            $"inline; filename=\"contract-{bookingId}.pdf\"";

        return File(result.Data!, "application/pdf");
    }

    // ── Contract detail page (HTML) ───────────────────────────────────────────

    [HttpGet("/Contract/{bookingId:int}")]
    public async Task<IActionResult> Details(
        int bookingId,
        CancellationToken ct)
    {
        var result = await _contractService.GetByBookingAsync(
            UserId, bookingId, IsAdmin, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Bookings",
                new { area = User.IsInRole("Student") ? "Student" : "Owner" });
        }

        return View("~/Views/Contract/Details.cshtml", result.Data);
    }

    // ── Admin: Regenerate contract ────────────────────────────────────────────

    [HttpPost("/Contract/Regenerate/{bookingId:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(
        int bookingId,
        CancellationToken ct)
    {
        var result = await _contractService.RegenerateAsync(bookingId, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return result.IsSuccess
                ? Ok(new
                {
                    success = true,
                    contractNumber = result.Data!.ContractNumber
                })
                : BadRequest(new { error = result.ErrorMessage });
        }

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Contract {result.Data!.ContractNumber} regenerated."
                : result.ErrorMessage;

        return RedirectToAction("Details", "Bookings",
            new { area = "Admin", id = bookingId });
    }
}