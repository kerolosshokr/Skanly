// Skanly.Web/Areas/Student/Controllers/ReviewsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Reviews.DTOs;
using Skanly.Application.Features.Reviews.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private string StudentId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── My Reviews List ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _reviewService.GetByStudentAsync(
            StudentId, page, 10, ct);

        return result.IsSuccess
            ? View(result.Data)
            : View("Error");
    }

    // ── Create Review ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create(
        int bookingId,
        int propertyId,
        CancellationToken ct)
    {
        // Check if already reviewed
        var existing = await _reviewService
            .GetByBookingAsync(StudentId, bookingId, ct);

        if (existing.IsSuccess && existing.Data is not null)
        {
            TempData["Info"] =
                "You have already reviewed this booking. " +
                "You can edit your existing review below.";
            return RedirectToAction(nameof(Edit),
                new { reviewId = existing.Data.ReviewId });
        }

        // Verify booking eligibility by attempting pre-check
        // (service validates fully on POST — this is just for UX)
        var dto = new CreateReviewDto
        {
            BookingId = bookingId,
            PropertyId = propertyId
        };

        ViewBag.BookingId = bookingId;
        ViewBag.PropertyId = propertyId;

        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateReviewDto dto,
        CancellationToken ct)
    {
        var result = await _reviewService.CreateAsync(StudentId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            ViewBag.BookingId = dto.BookingId;
            ViewBag.PropertyId = dto.PropertyId;
            return View(dto);
        }

        TempData["Success"] =
            "Thank you! Your review has been published.";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit Review ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(
        int reviewId,
        CancellationToken ct)
    {
        var result = await _reviewService.GetByStudentAsync(StudentId, 1, 100, ct);

        var review = result.Data?.Items
            .FirstOrDefault(r => r.ReviewId == reviewId);

        if (review is null)
        {
            TempData["Error"] = "Review not found.";
            return RedirectToAction(nameof(Index));
        }

        if (!review.CanEdit)
        {
            TempData["Error"] = review.IsHidden
                ? "This review is hidden and cannot be edited."
                : review.UpdatedAt.HasValue
                    ? "You have already edited this review once."
                    : "The 30-day edit window for this review has passed.";
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateReviewDto
        {
            ReviewId = review.ReviewId,
            CleanlinessRating = review.CleanlinessRating,
            SafetyRating = review.SafetyRating,
            InternetRating = review.InternetRating,
            LocationRating = review.LocationRating,
            QuietnessRating = review.QuietnessRating,
            OverallRating = review.OverallRating,
            Comment = review.Comment
        };

        ViewBag.Review = review;
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UpdateReviewDto dto,
        CancellationToken ct)
    {
        var result = await _reviewService.UpdateAsync(StudentId, dto, ct);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!result.Errors.Any())
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            // Reload review for the view
            var reviewResult = await _reviewService
                .GetByStudentAsync(StudentId, 1, 100, ct);
            ViewBag.Review = reviewResult.Data?.Items
                .FirstOrDefault(r => r.ReviewId == dto.ReviewId);

            return View(dto);
        }

        TempData["Success"] = "Your review has been updated.";
        return RedirectToAction(nameof(Index));
    }

    // ── Delete Review ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int reviewId,
        CancellationToken ct)
    {
        var result = await _reviewService.DeleteAsync(StudentId, reviewId, ct);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? "Your review has been removed."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    // ── AJAX: Check if booking has been reviewed ──────────────────────────────

    [HttpGet]
    public async Task<IActionResult> CheckBooking(
        int bookingId,
        CancellationToken ct)
    {
        var result = await _reviewService
            .GetByBookingAsync(StudentId, bookingId, ct);

        return Ok(new
        {
            hasReview = result.IsSuccess && result.Data != null,
            reviewId = result.Data?.ReviewId
        });
    }
}