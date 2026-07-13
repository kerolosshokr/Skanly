// Skanly.Application/Features/Reviews/Services/ReviewService.cs
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Reviews.DTOs;
using Skanly.Application.Features.Reviews.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Reviews.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateReviewDto> _createValidator;
    private readonly IValidator<UpdateReviewDto> _updateValidator;
    private readonly ILogger<ReviewService> _logger;

    // Business rule constants
    private const int EditWindowDays = 30;
    private const int MaxEditsPerReview = 1;

    public ReviewService(
        IUnitOfWork uow,
        IValidator<CreateReviewDto> createValidator,
        IValidator<UpdateReviewDto> updateValidator,
        ILogger<ReviewService> logger)
    {
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<ReviewDto>> CreateAsync(
        string studentId,
        CreateReviewDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate fields
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<ReviewDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Load booking with full details
        var booking = await _uow.Bookings.GetDetailAsync(dto.BookingId, ct);

        if (booking is null)
            return ServiceResult<ReviewDto>.Failure("Booking not found.");

        // 3. Verify booking belongs to this student
        if (booking.StudentId != studentId)
            return ServiceResult<ReviewDto>.Failure(
                "You can only review your own bookings.");

        // 4. Verify booking is confirmed (spec: only verified bookings can review)
        if (booking.Status != BookingStatus.Confirmed)
            return ServiceResult<ReviewDto>.Failure(
                "Reviews are only available for confirmed bookings. " +
                "Your booking must be fully confirmed before you can leave a review.");

        // 5. Verify property matches booking
        if (booking.PropertyId != dto.PropertyId)
            return ServiceResult<ReviewDto>.Failure(
                "Property does not match this booking.");

        // 6. Idempotency — one review per booking
        var alreadyReviewed = await _uow.Reviews
            .HasReviewedBookingAsync(dto.BookingId, ct);

        if (alreadyReviewed)
            return ServiceResult<ReviewDto>.Failure(
                "You have already submitted a review for this booking. " +
                "You can edit your existing review instead.");

        // 7. Create review
        var review = new Review
        {
            BookingId = dto.BookingId,
            StudentId = studentId,
            PropertyId = dto.PropertyId,
            CleanlinessRating = dto.CleanlinessRating,
            SafetyRating = dto.SafetyRating,
            InternetRating = dto.InternetRating,
            LocationRating = dto.LocationRating,
            QuietnessRating = dto.QuietnessRating,
            OverallRating = dto.OverallRating,
            Comment = dto.Comment?.Trim(),
            IsHidden = false
        };

        await _uow.Reviews.AddAsync(review, ct);
        await _uow.SaveChangesAsync(ct);

        // 8. Recalculate property average rating
        await RecalculateAndSaveAverageAsync(dto.PropertyId, ct);

        _logger.LogInformation(
            "Review {ReviewId} created for Property {PropertyId} by Student {StudentId}",
            review.Id, dto.PropertyId, studentId);

        return await BuildReviewDtoResultAsync(review.Id, ct);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<ReviewDto>> UpdateAsync(
        string studentId,
        UpdateReviewDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<ReviewDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Load review
        var review = await _uow.Reviews.GetByIdAsync(dto.ReviewId, ct);

        if (review is null)
            return ServiceResult<ReviewDto>.Failure("Review not found.");

        // 3. Ownership check
        if (review.StudentId != studentId)
            return ServiceResult<ReviewDto>.Failure(
                "You can only edit your own reviews.");

        // 4. Moderation check
        if (review.IsHidden)
            return ServiceResult<ReviewDto>.Failure(
                "This review has been hidden by our moderation team " +
                "and cannot be edited.");

        // 5. One-edit-only rule
        if (review.UpdatedAt.HasValue)
            return ServiceResult<ReviewDto>.Failure(
                "Reviews can only be edited once. " +
                "Your review has already been updated.");

        // 6. 30-day window
        var daysSinceCreation = (DateTime.UtcNow - review.CreatedAt).TotalDays;
        if (daysSinceCreation > EditWindowDays)
            return ServiceResult<ReviewDto>.Failure(
                $"Reviews can only be edited within {EditWindowDays} days of " +
                "submission. The edit window for this review has closed.");

        // 7. Apply changes
        review.CleanlinessRating = dto.CleanlinessRating;
        review.SafetyRating = dto.SafetyRating;
        review.InternetRating = dto.InternetRating;
        review.LocationRating = dto.LocationRating;
        review.QuietnessRating = dto.QuietnessRating;
        review.OverallRating = dto.OverallRating;
        review.Comment = dto.Comment?.Trim();
        review.UpdatedAt = DateTime.UtcNow;

        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync(ct);

        // 8. Recalculate average
        await RecalculateAndSaveAverageAsync(review.PropertyId, ct);

        _logger.LogInformation(
            "Review {ReviewId} updated by Student {StudentId}",
            review.Id, studentId);

        return await BuildReviewDtoResultAsync(review.Id, ct);
    }

    // ── DeleteAsync (Student) ─────────────────────────────────────────────────

    public async Task<ServiceResult> DeleteAsync(
        string studentId,
        int reviewId,
        CancellationToken ct = default)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId, ct);

        if (review is null)
            return ServiceResult.Failure("Review not found.");

        if (review.StudentId != studentId)
            return ServiceResult.Failure(
                "You can only delete your own reviews.");

        if (review.IsHidden)
            return ServiceResult.Failure(
                "This review has been hidden and cannot be deleted by you. " +
                "Please contact support.");

        var daysSince = (DateTime.UtcNow - review.CreatedAt).TotalDays;
        if (daysSince > EditWindowDays)
            return ServiceResult.Failure(
                $"Reviews can only be deleted within {EditWindowDays} days " +
                "of submission.");

        var propertyId = review.PropertyId;

        _uow.Reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);

        await RecalculateAndSaveAverageAsync(propertyId, ct);

        _logger.LogInformation(
            "Review {ReviewId} deleted by Student {StudentId}",
            reviewId, studentId);

        return ServiceResult.Success();
    }

    // ── GetByStudentAsync ─────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<ReviewDto>>> GetByStudentAsync(
        string studentId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var allReviews = await _uow.Reviews
            .GetByStudentIdAsync(studentId, ct);

        var total = allReviews.Count;
        var paged = allReviews
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = await BuildReviewDtosAsync(paged, ct);

        return ServiceResult<PagedResult<ReviewDto>>.Success(
            PagedResult<ReviewDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── GetByBookingAsync ─────────────────────────────────────────────────────

    public async Task<ServiceResult<ReviewDto?>> GetByBookingAsync(
        string studentId,
        int bookingId,
        CancellationToken ct = default)
    {
        // Verify booking belongs to student
        var booking = await _uow.Bookings
            .GetFirstOrDefaultAsync(b => b.Id == bookingId &&
                                         b.StudentId == studentId, ct);

        if (booking is null)
            return ServiceResult<ReviewDto?>.Failure("Booking not found.");

        var review = await _uow.Reviews.GetByBookingIdAsync(bookingId, ct);
        if (review is null)
            return ServiceResult<ReviewDto?>.Success(null);

        var dto = await MapSingleReviewAsync(review, ct);
        return ServiceResult<ReviewDto?>.Success(dto);
    }

    // ── GetByPropertyAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<(IReadOnlyList<ReviewDto> Reviews,
                                     ReviewSummaryDto Summary)>> GetByPropertyAsync(
        int propertyId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var allReviews = await _uow.Reviews
            .GetByPropertyIdAsync(propertyId, ct);

        // Exclude hidden reviews from public display
        var visible = allReviews
            .Where(r => !r.IsHidden)
            .ToList();

        var total = visible.Count;
        var paged = visible
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = await BuildReviewDtosAsync(paged, ct);
        var summary = BuildSummary(propertyId, visible);

        return ServiceResult<(IReadOnlyList<ReviewDto>, ReviewSummaryDto)>
            .Success((dtos, summary));
    }

    // ── GetSummaryAsync ───────────────────────────────────────────────────────

    public async Task<ServiceResult<ReviewSummaryDto>> GetSummaryAsync(
        int propertyId,
        CancellationToken ct = default)
    {
        var reviews = await _uow.Reviews
            .GetByPropertyIdAsync(propertyId, ct);

        var visible = reviews.Where(r => !r.IsHidden).ToList();
        var summary = BuildSummary(propertyId, visible);

        return ServiceResult<ReviewSummaryDto>.Success(summary);
    }

    // ── GetAllAsync (Admin) ───────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<ReviewDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? searchTerm = null,
        bool? hiddenOnly = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _uow.Repository<Review>()
            .GetPagedAsync(
                pageNumber,
                pageSize,
                predicate: r =>
                    (hiddenOnly == null || r.IsHidden == hiddenOnly) &&
                    (searchTerm == null ||
                     r.Student.FirstName.Contains(searchTerm) ||
                     r.Student.LastName.Contains(searchTerm) ||
                     r.Property.Title.Contains(searchTerm) ||
                     (r.Comment != null && r.Comment.Contains(searchTerm))),
                orderBy: q => q.OrderByDescending(r => r.CreatedAt),
                ct: ct,
                r => r.Student,
                r => r.Property,
                r => r.Property.Area,
                r => r.Property.Images);

        var dtos = await BuildReviewDtosAsync(items.ToList(), ct);

        return ServiceResult<PagedResult<ReviewDto>>.Success(
            PagedResult<ReviewDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── HideAsync (Admin) ─────────────────────────────────────────────────────

    public async Task<ServiceResult> HideAsync(
        int reviewId,
        CancellationToken ct = default)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.Failure("Review not found.");

        if (review.IsHidden)
            return ServiceResult.Failure("Review is already hidden.");

        review.IsHidden = true;
        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync(ct);

        // Recalculate without this review
        await RecalculateAndSaveAverageAsync(review.PropertyId, ct);

        _logger.LogInformation(
            "Review {ReviewId} hidden by Admin for Property {PropertyId}",
            reviewId, review.PropertyId);

        return ServiceResult.Success();
    }

    // ── UnhideAsync (Admin) ───────────────────────────────────────────────────

    public async Task<ServiceResult> UnhideAsync(
        int reviewId,
        CancellationToken ct = default)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.Failure("Review not found.");

        if (!review.IsHidden)
            return ServiceResult.Failure("Review is already visible.");

        review.IsHidden = false;
        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync(ct);

        await RecalculateAndSaveAverageAsync(review.PropertyId, ct);

        _logger.LogInformation(
            "Review {ReviewId} unhidden by Admin", reviewId);

        return ServiceResult.Success();
    }

    // ── AdminDeleteAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult> AdminDeleteAsync(
        int reviewId,
        CancellationToken ct = default)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.Failure("Review not found.");

        var propertyId = review.PropertyId;

        _uow.Reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);

        await RecalculateAndSaveAverageAsync(propertyId, ct);

        _logger.LogInformation(
            "Review {ReviewId} permanently deleted by Admin", reviewId);

        return ServiceResult.Success();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task RecalculateAndSaveAverageAsync(
        int propertyId,
        CancellationToken ct)
    {
        var newAvg = await _uow.Reviews
            .CalculateAverageRatingAsync(propertyId, ct);

        // Push updated average back to the denormalized column on Property
        await _uow.Properties.RecalculateAverageRatingAsync(propertyId, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Property {PropertyId} average rating recalculated to {Avg}",
            propertyId, newAvg);
    }

    private static ReviewSummaryDto BuildSummary(
        int propertyId,
        IReadOnlyList<Review> visible)
    {
        if (!visible.Any())
            return new ReviewSummaryDto { PropertyId = propertyId };

        var starDist = new Dictionary<int, int>
        {
            [5] = visible.Count(r => r.OverallRating == 5),
            [4] = visible.Count(r => r.OverallRating == 4),
            [3] = visible.Count(r => r.OverallRating == 3),
            [2] = visible.Count(r => r.OverallRating == 2),
            [1] = visible.Count(r => r.OverallRating == 1)
        };

        return new ReviewSummaryDto
        {
            PropertyId = propertyId,
            TotalReviews = visible.Count,
            OverallAverage = Round(visible.Average(r => r.OverallRating)),
            CleanlinessAverage = Round(visible.Average(r => r.CleanlinessRating)),
            SafetyAverage = Round(visible.Average(r => r.SafetyRating)),
            InternetAverage = Round(visible.Average(r => r.InternetRating)),
            LocationAverage = Round(visible.Average(r => r.LocationRating)),
            QuietnessAverage = Round(visible.Average(r => r.QuietnessRating)),
            StarDistribution = starDist
        };
    }

    private async Task<ServiceResult<ReviewDto>> BuildReviewDtoResultAsync(
        int reviewId,
        CancellationToken ct)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult<ReviewDto>.Failure("Review not found after save.");

        var dto = await MapSingleReviewAsync(review, ct);
        return ServiceResult<ReviewDto>.Success(dto);
    }

    private async Task<IReadOnlyList<ReviewDto>> BuildReviewDtosAsync(
        List<Review> reviews,
        CancellationToken ct)
    {
        var dtos = new List<ReviewDto>();
        foreach (var r in reviews)
            dtos.Add(await MapSingleReviewAsync(r, ct));
        return dtos;
    }

    private async Task<ReviewDto> MapSingleReviewAsync(
        Review r,
        CancellationToken ct)
    {
        // Load navigations if not already loaded
        var student = r.Student
            ?? await _uow.Students.GetByUserIdAsync(r.StudentId, ct);

        var property = r.Property
            ?? await _uow.Properties.GetDetailAsync(r.PropertyId, ct);

        return new ReviewDto
        {
            ReviewId = r.Id,
            BookingId = r.BookingId,
            StudentId = r.StudentId,
            StudentFullName = student?.FullName ?? "Student",
            StudentImageUrl = student?.ProfileImageUrl,
            PropertyId = r.PropertyId,
            PropertyTitle = property?.Title ?? string.Empty,
            PropertyImageUrl = property?.Images
                                    .FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            AreaNameEn = property?.Area.NameEn ?? string.Empty,
            CleanlinessRating = r.CleanlinessRating,
            SafetyRating = r.SafetyRating,
            InternetRating = r.InternetRating,
            LocationRating = r.LocationRating,
            QuietnessRating = r.QuietnessRating,
            OverallRating = r.OverallRating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsHidden = r.IsHidden
        };
    }

    private static decimal Round(double value)
        => Math.Round((decimal)value, 2);
}