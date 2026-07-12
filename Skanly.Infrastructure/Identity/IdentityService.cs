// Skanly.Infrastructure/Identity/IdentityService.cs
using Microsoft.AspNetCore.Identity;
using Skanly.Application.Common.Interfaces;

namespace Skanly.Infrastructure.Identity;

/// <summary>
/// Wraps ASP.NET Identity's UserManager behind the IIdentityService interface.
/// This is the ONLY place in the codebase where UserManager is used directly.
/// Application layer never sees UserManager or ApplicationUser.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetEmailAsync(
        string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.Email;
    }

    public async Task<string?> GetPhoneNumberAsync(
        string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.PhoneNumber;
    }

    public async Task<bool> UpdatePhoneNumberAsync(
        string userId, string phoneNumber, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        user.PhoneNumber = phoneNumber;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> UserExistsAsync(
        string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is not null;
    }

    public async Task<bool> IsActiveAsync(
        string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.IsActive ?? false;
    }

    public async Task<bool> IsInRoleAsync(
        string userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<string?> GetFullNameFromIdentityAsync(
        string userId, CancellationToken ct = default)
    {
        // Identity user doesn't hold FirstName/LastName —
        // that's in the Student/Owner profile tables.
        // This returns the username as fallback.
        var user = await _userManager.FindByIdAsync(userId);
        return user?.UserName;
    }
    // ── الدوال الجديدة ────────────────────────────────────────────────────────
    public async Task<bool> DeactivateUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ActivateUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        user.IsActive = true;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}