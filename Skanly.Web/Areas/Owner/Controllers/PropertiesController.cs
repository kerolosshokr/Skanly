using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Owners.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Policy = "OwnerOnly")]
public class PropertiesController : Controller
{
    private readonly IOwnerService _ownerService;

    public PropertiesController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? status = null,
        CancellationToken ct = default)
    {
        var result = await _ownerService
            .GetPropertiesAsync(UserId, page, pageSize, status, ct);

        ViewBag.StatusFilter = status;
        ViewBag.PageSize     = pageSize;

        return result.IsSuccess ? View(result.Data) : View("Error");
    }
}
