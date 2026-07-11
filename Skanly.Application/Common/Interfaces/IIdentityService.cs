// Skanly.Application/Common/Interfaces/IIdentityService.cs
namespace Skanly.Application.Common.Interfaces;

/// <summary>
/// Application layer's abstraction over ASP.NET Identity.
/// StudentService and other services inject this — never UserManager directly.
/// The Infrastructure layer provides the implementation.
/// </summary>
public interface IIdentityService
{
    Task<string?> GetEmailAsync(string userId, CancellationToken ct = default);
    Task<string?> GetPhoneNumberAsync(string userId, CancellationToken ct = default);
    Task<bool> UpdatePhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default);
    Task<bool> UserExistsAsync(string userId, CancellationToken ct = default);
    Task<bool> IsActiveAsync(string userId, CancellationToken ct = default);
    Task<bool> IsInRoleAsync(string userId, string role, CancellationToken ct = default);
    Task<string?> GetFullNameFromIdentityAsync(string userId, CancellationToken ct = default);
}