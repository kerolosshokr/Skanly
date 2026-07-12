// Skanly.Web/Areas/Owner/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Owners.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class DashboardController : Controller
{
    private readonly IOwnerService _ownerService;

    public DashboardController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _ownerService.GetDashboardAsync(UserId, ct);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return View("Error");
        }

        return View(result.Data);
    }
}