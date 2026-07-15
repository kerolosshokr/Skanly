// Skanly.Web/Areas/Owner/Controllers/EarningsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Owners.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class EarningsController : Controller
{
    private readonly IOwnerService _ownerService;
    private readonly IUnitOfWork _uow;

    public EarningsController(
        IOwnerService ownerService,
        IUnitOfWork uow)
    {
        _ownerService = ownerService;
        _uow = uow;
    }

    private string OwnerId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        int year = 0,
        CancellationToken ct = default)
    {
        if (year == 0) year = DateTime.UtcNow.Year;

        var result = await _ownerService
            .GetEarningsAsync(OwnerId, year, ct);

        var owner = await _uow.Owners.GetByUserIdAsync(OwnerId, ct);
        ViewBag.OwnerFullName = owner?.FullName;
        ViewBag.OwnerImageUrl = owner?.ProfileImageUrl;
        ViewBag.OwnerIsVerified = owner?.IsIdentityVerified ?? false;
        ViewBag.Year = year;
        ViewBag.AvailableYears = Enumerable
            .Range(DateTime.UtcNow.Year - 2, 3)
            .Reverse()
            .ToList();

        return result.IsSuccess ? View(result.Data) : View("Error");
    }
}