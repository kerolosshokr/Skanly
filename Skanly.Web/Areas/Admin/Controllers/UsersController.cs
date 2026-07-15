// Skanly.Web/Areas/Admin/Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Application.Common.Models;
using Skanly.Infrastructure.Identity;

namespace Skanly.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IIdentityService _identityService;

    public UsersController(
        IUnitOfWork uow,
        IIdentityService identityService)
    {
        _uow = uow;
        _identityService = identityService;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        string? search = null,
        string? role = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var students = await _uow.Repository<Skanly.Domain.Entities.Student>()
      .GetAllAsync(ct);

        var owners = await _uow.Repository<Skanly.Domain.Entities.Owner>()
      .GetAllAsync(ct);
        var users = await _uow.Repository<ApplicationUser>()
            .GetAllAsync(ct);

        // Build combined user list
        var userRows = new List<AdminUserRow>();

        foreach (var user in users)
        {
            var student = students.FirstOrDefault(s => s.UserId == user.Id);
            var owner = owners.FirstOrDefault(o => o.UserId == user.Id);

            var userRole = student is not null ? "Student"
                         : owner is not null ? "Owner" : "Admin";

            var fullName = student?.FullName ?? owner?.FullName ?? user.UserName ?? "";
            var isVerified = student?.IsIdentityVerified ??
                             owner?.IsIdentityVerified ?? false;

            if (!string.IsNullOrEmpty(search) &&
                !fullName.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                !user.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                continue;

            if (!string.IsNullOrEmpty(role) &&
                !userRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                continue;

            if (status == "active" && !user.IsActive) continue;
            if (status == "inactive" && user.IsActive) continue;

            userRows.Add(new AdminUserRow
            {
                UserId = user.Id,
                FullName = fullName,
                Email = user.Email ?? "",
                Role = userRole,
                IsVerified = isVerified,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                ProfileImage = student?.ProfileImageUrl
                               ?? owner?.ProfileImageUrl
            });
        }

        userRows = userRows.OrderByDescending(u => u.CreatedAt).ToList();

        int pageSize = 20;
        var total = userRows.Count;
        var paged = userRows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.Status = status;
        ViewBag.TotalStudents = students.Count;
        ViewBag.TotalOwners = owners.Count;

        return View(PagedResult<AdminUserRow>.Create(
            paged, total, page, pageSize));
    }

    // ── Deactivate / Activate ─────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(
        string userId, CancellationToken ct)
    {
        var success = await _identityService
            .DeactivateUserAsync(userId, ct);

        return Json(new { success });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(
        string userId, CancellationToken ct)
    {
        var success = await _identityService
            .ActivateUserAsync(userId, ct);

        return Json(new { success });
    }

    // ── Detail ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Detail(
        string id, CancellationToken ct)
    {
        var student = await _uow.Students
            .GetWithUniversityAsync(id, ct);
        var owner = student is null
            ? await _uow.Owners.GetByUserIdAsync(id, ct)
            : null;

        var email = await _identityService.GetEmailAsync(id, ct);
        var phone = await _identityService.GetPhoneNumberAsync(id, ct);

        var bookings = await _uow.Repository<Booking>()
            .GetAllAsync(b => b.StudentId == id, ct);

        var verifications = await _uow.Repository<IdentityVerification>()
            .GetAllAsync(v => v.UserId == id, ct);

        ViewBag.Student = student;
        ViewBag.Owner = owner;
        ViewBag.Email = email;
        ViewBag.Phone = phone;
        ViewBag.Bookings = bookings;
        ViewBag.Verifications = verifications;

        return View();
    }
}

public class AdminUserRow
{
    public string UserId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ProfileImage { get; init; }
    public string TimeAgo
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt;
            return span.TotalDays < 1 ? "Today"
                 : span.TotalDays < 7 ? $"{(int)span.TotalDays}d ago"
                 : CreatedAt.ToString("MMM dd, yyyy");
        }
    }
}