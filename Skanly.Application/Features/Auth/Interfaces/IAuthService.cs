// Skanly.Application/Features/Auth/Interfaces/IAuthService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Auth.DTOs;

namespace Skanly.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResultDto>> RegisterStudentAsync(
        RegisterStudentDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<AuthResultDto>> RegisterOwnerAsync(
        RegisterOwnerDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<AuthResultDto>> LoginAsync(
        LoginDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<AuthResultDto>> RefreshTokenAsync(
        RefreshTokenDto dto,
        CancellationToken ct = default);

    Task<ServiceResult> ConfirmEmailAsync(
        string userId,
        string token,
        CancellationToken ct = default);

    Task<ServiceResult> ForgotPasswordAsync(
        ForgotPasswordDto dto,
        CancellationToken ct = default);

    Task<ServiceResult> ResetPasswordAsync(
        ResetPasswordDto dto,
        CancellationToken ct = default);

    Task<ServiceResult> RevokeRefreshTokenAsync(
        string userId,
        CancellationToken ct = default);

    Task<ServiceResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);
}