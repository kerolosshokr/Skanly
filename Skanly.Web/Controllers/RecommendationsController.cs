// Skanly.Web/Controllers/RecommendationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Recommendations.DTOs;
using Skanly.Application.Features.Recommendations.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[Authorize(Policy = "StudentOnly")]
public class RecommendationsController : Controller
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(
        IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    private string StudentId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Recommendations Page ──────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        RecommendationRequestDto? request = null,
        CancellationToken ct = default)
    {
        var result = await _recommendationService
            .GetRecommendationsAsync(StudentId, request, ct);

        // Load profile stats for the page header
        var profileResult = await _recommendationService
            .GetPreferenceProfileAsync(StudentId, ct);

        ViewBag.Profile = profileResult.Data;

        return result.IsSuccess
            ? View(result.Data)
            : View("Error");
    }

    // ── AJAX: Refresh recommendations ─────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(
        RecommendationRequestDto request,
        CancellationToken ct)
    {
        request.ForceRefresh = true;

        var result = await _recommendationService
            .GetRecommendationsAsync(StudentId, request, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }

    // ── AJAX: Preference profile (for debug or "Why am I seeing this?") ───────

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var result = await _recommendationService
            .GetPreferenceProfileAsync(StudentId, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }
}