// Skanly.Application/Common/Interfaces/IIdentityService.cs
using Skanly.Application.Common.DTOs;
namespace Skanly.Application.Common.Interfaces;

/// <summary>
/// Application layer abstraction over ASP.NET Identity.
/// Services should depend on this interface instead of UserManager directly.
/// </summary>
public interface IIdentityService
{
    // ─────────────────────────────────────────────────────────────
    // User Information
    // ─────────────────────────────────────────────────────────────

    Task<string?> GetEmailAsync(
        string userId,
        CancellationToken ct = default);

    Task<string?> GetPhoneNumberAsync(
        string userId,
        CancellationToken ct = default);

    Task<string?> GetFullNameFromIdentityAsync(
        string userId,
        CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────
    // User Management
    // ─────────────────────────────────────────────────────────────

    Task<bool> UpdatePhoneNumberAsync(
        string userId,
        string phoneNumber,
        CancellationToken ct = default);

    Task<bool> UserExistsAsync(
        string userId,
        CancellationToken ct = default);

    Task<bool> IsActiveAsync(
        string userId,
        CancellationToken ct = default);

    Task<bool> IsInRoleAsync(
        string userId,
        string role,
        CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────
    // Admin / Owner Operations
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Deactivates a user account (IsActive = false).
    /// </summary>
    Task<bool> DeactivateUserAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Reactivates a previously deactivated user account.
    /// </summary>
    Task<bool> ActivateUserAsync(
        string userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<IdentityUserDto>> GetAllUsersAsync(
    CancellationToken ct = default);
}