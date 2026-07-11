using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Owners.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class EarningsController : Controller
{
    private readonly IOwnerService _ownerService;

    public EarningsController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        int year = 0,
        CancellationToken ct = default)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        var result = await _ownerService.GetEarningsAsync(UserId, year, ct);

        ViewBag.Year         = year;
        ViewBag.PreviousYear = year - 1;
        ViewBag.NextYear     = year + 1;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }
}
