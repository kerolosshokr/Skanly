// Skanly.Web/Areas/Admin/Controllers/PaymentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Payments.Interfaces;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        string? status = null,
        string? method = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        // Default date range: last 30 days
        var dateFrom = from ?? DateTime.UtcNow.AddDays(-30);
        var dateTo = to ?? DateTime.UtcNow;

        var result = await _paymentService.GetAllPaymentsAsync(
            page, 20, status, method, dateFrom, dateTo, ct);

        var summary = await _paymentService
            .GetPaymentSummaryAsync(dateFrom, dateTo, ct);

        ViewBag.StatusFilter = status;
        ViewBag.MethodFilter = method;
        ViewBag.From = dateFrom.ToString("yyyy-MM-dd");
        ViewBag.To = dateTo.ToString("yyyy-MM-dd");
        ViewBag.Summary = summary.Data;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }
}