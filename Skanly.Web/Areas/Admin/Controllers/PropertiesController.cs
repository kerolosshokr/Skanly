// Skanly.Web/Areas/Admin/Controllers/PropertiesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Domain.Enums;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class PropertiesController : Controller
{
    private readonly IPropertyService _propertyService;

    public PropertiesController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }
    
    // ── Pending Approval Queue ────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Pending(
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _propertyService.GetPendingApprovalAsync(page, 10, ct);
        return result.IsSuccess ? View(result.Data) : View("Error");
    }

    // ── Detail for review ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Review(int id, CancellationToken ct)
    {
        var result = await _propertyService.GetDetailAsync(id, null, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Pending));
        }
        return View(result.Data);
    }

    // ── Approve (AJAX) ────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var result = await _propertyService.ApproveAsync(id, ct);
        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }

    // ── Reject (AJAX) ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        string reason,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Json(new { success = false, message = "Rejection reason is required." });

        var result = await _propertyService.RejectAsync(id, reason, ct);
        return result.IsSuccess
            ? Json(new { success = true })
            : Json(new { success = false, message = result.ErrorMessage });
    }
}