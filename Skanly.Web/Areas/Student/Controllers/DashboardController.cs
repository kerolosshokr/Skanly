// Skanly.Web/Areas/Student/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Application.Features.Recommendations.Interfaces;
using Skanly.Application.Features.Students.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using System.Security.Claims;

namespace Skanly.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Policy = "StudentOnly")]
public class DashboardController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IBookingService _bookingService;
    private readonly IRecommendationService _recommendationService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public DashboardController(
        IStudentService studentService,
        IBookingService bookingService,
        IRecommendationService recommendationService,
        INotificationService notificationService,
        IUnitOfWork uow)
    {
        _studentService = studentService;
        _bookingService = bookingService;
        _recommendationService = recommendationService;
        _notificationService = notificationService;
        _uow = uow;
    }

    private string StudentId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Load profile
        var profileResult = await _studentService
            .GetProfileAsync(StudentId, ct);

        if (!profileResult.IsSuccess)
        {
            TempData["Error"] = profileResult.ErrorMessage;
            return View(null);
        }

        var profile = profileResult.Data!;

        // Recent bookings (last 3)
        var bookingsResult = await _bookingService.GetByStudentAsync(
            StudentId, 1, 3, null, ct);

        // AI recommendations preview (top 4)
        var recsResult = await _recommendationService
            .GetRecommendationsAsync(StudentId,
                new Application.Features.Recommendations.DTOs
                    .RecommendationRequestDto
                {
                    MaxResults = 4,
                    IncludeExplanations = false
                }, ct);

        // Unread counts
        var unreadNotif = await _notificationService
            .GetUnreadCountAsync(StudentId, ct);
        var unreadMsg = await _uow.Chat.GetUnreadCountAsync(StudentId, ct);
        var favCount = await _uow.Favorites.CountAsync(
            f => f.StudentId == StudentId, ct);
        var activeBookCount = await _uow.Repository<Booking>()
            .CountAsync(b => b.StudentId == StudentId &&
                             (b.Status == BookingStatus.Pending ||
                              b.Status == BookingStatus.Accepted ||
                              b.Status == BookingStatus.PaymentPending), ct);

        // Sidebar context
        ViewBag.StudentFullName = profile.FullName;
        ViewBag.StudentImageUrl = profile.ProfileImageUrl;
        ViewBag.StudentIsVerified = profile.IsIdentityVerified;
        ViewBag.UniversityName = profile.UniversityNameEn;
        ViewBag.FavoritesCount = favCount;
        ViewBag.ActiveBookingsCount = activeBookCount;
        ViewBag.UnreadNotifications = unreadNotif.Data;
        ViewBag.UnreadMessages = unreadMsg;

        // Page data
        ViewBag.Recommendations = recsResult.Data;
        ViewBag.RecentBookings = bookingsResult.Data?.Items;

        return View(profile);
    }
}