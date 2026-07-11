// Skanly.Infrastructure/Identity/AuthService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Auth.DTOs;
using Skanly.Application.Features.Auth.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Skanly.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService jwtService,
        IEmailService emailService,
        IUnitOfWork uow,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _emailService = emailService;
        _uow = uow;
        _config = config;
        _logger = logger;
    }

    // ── Register Student ──────────────────────────────────────────────────────

    public async Task<ServiceResult<AuthResultDto>> RegisterStudentAsync(
        RegisterStudentDto dto,
        CancellationToken ct = default)
    {
        // Check email uniqueness
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return ServiceResult<AuthResultDto>.Failure("Email is already registered.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            // 1. Create Identity user
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var identityResult = await _userManager.CreateAsync(user, dto.Password);
            if (!identityResult.Succeeded)
                return ServiceResult<AuthResultDto>.Failure(
                    identityResult.Errors.Select(e => e.Description).ToList());

            // 2. Assign Student role
            await _userManager.AddToRoleAsync(user, "Student");

            // 3. Create Student profile (Part 3 entity)
            var student = new Student
            {
                UserId = user.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Gender = (Gender)dto.Gender,
                UniversityId = dto.UniversityId
            };

            await _uow.Students.AddAsync(student, ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // 4. Send confirmation email (outside transaction — non-critical)
            await SendConfirmationEmailAsync(user, ct);

            _logger.LogInformation(
                "Student registered: {Email} | Id: {UserId}", user.Email, user.Id);

            return ServiceResult<AuthResultDto>.Success(
                BuildAuthResult(user, "Student", $"{student.FirstName} {student.LastName}"));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Student registration failed for {Email}", dto.Email);
            return ServiceResult<AuthResultDto>.Failure("Registration failed. Please try again.");
        }
    }

    // ── Register Owner ────────────────────────────────────────────────────────

    public async Task<ServiceResult<AuthResultDto>> RegisterOwnerAsync(
        RegisterOwnerDto dto,
        CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return ServiceResult<AuthResultDto>.Failure("Email is already registered.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var identityResult = await _userManager.CreateAsync(user, dto.Password);
            if (!identityResult.Succeeded)
                return ServiceResult<AuthResultDto>.Failure(
                    identityResult.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(user, "Owner");

            var owner = new Owner
            {
                UserId = user.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                BusinessName = dto.BusinessName
            };

            await _uow.Owners.AddAsync(owner, ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await SendConfirmationEmailAsync(user, ct);

            _logger.LogInformation(
                "Owner registered: {Email} | Id: {UserId}", user.Email, user.Id);

            return ServiceResult<AuthResultDto>.Success(
                BuildAuthResult(user, "Owner", $"{owner.FirstName} {owner.LastName}"));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Owner registration failed for {Email}", dto.Email);
            return ServiceResult<AuthResultDto>.Failure("Registration failed. Please try again.");
        }
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<AuthResultDto>> LoginAsync(
        LoginDto dto,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
            return ServiceResult<AuthResultDto>.Failure("Invalid email or password.");

        if (!user.IsActive)
            return ServiceResult<AuthResultDto>.Failure("Your account has been deactivated. Please contact support.");

        if (!user.EmailConfirmed)
            return ServiceResult<AuthResultDto>.Failure("Please confirm your email before logging in.");

        // CheckPasswordSignInAsync handles lockout automatically
        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user, dto.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
            return ServiceResult<AuthResultDto>.Failure(
                "Account locked after multiple failed attempts. Try again in 15 minutes.");

        if (signInResult.IsNotAllowed)
            return ServiceResult<AuthResultDto>.Failure("Login is not allowed for this account.");

        if (!signInResult.Succeeded)
            return ServiceResult<AuthResultDto>.Failure("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Student";

        // Build JWT claims
        var claims = BuildClaims(user, role);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(claims);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = _jwtService.GetRefreshTokenExpiry();

        // Store refresh token on the user
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshExpiry;
        await _userManager.UpdateAsync(user);

        // Get full name from role profile
        var fullName = await GetFullNameAsync(user.Id, role, ct);
        var profileImage = await GetProfileImageAsync(user.Id, role, ct);

        _logger.LogInformation("User logged in: {Email} | Role: {Role}", user.Email, role);

        return ServiceResult<AuthResultDto>.Success(new AuthResultDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FullName = fullName,
            Role = role,
            AccessToken = accessToken,
            AccessTokenExpiry = _jwtService.GetAccessTokenExpiry(),
            RefreshToken = refreshToken,
            RefreshTokenExpiry = refreshExpiry,
            ProfileImageUrl = profileImage
        });
    }

    // ── Refresh Token ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<AuthResultDto>> RefreshTokenAsync(
        RefreshTokenDto dto,
        CancellationToken ct = default)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal is null)
            return ServiceResult<AuthResultDto>.Failure("Invalid access token.");

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return ServiceResult<AuthResultDto>.Failure("Invalid token claims.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null ||
            user.RefreshToken != dto.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return ServiceResult<AuthResultDto>.Failure(
                "Invalid or expired refresh token. Please log in again.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Student";
        var claims = BuildClaims(user, role);

        var newAccessToken = _jwtService.GenerateAccessToken(claims);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshExpiry = _jwtService.GetRefreshTokenExpiry();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = newRefreshExpiry;
        await _userManager.UpdateAsync(user);

        var fullName = await GetFullNameAsync(user.Id, role, ct);

        return ServiceResult<AuthResultDto>.Success(new AuthResultDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FullName = fullName,
            Role = role,
            AccessToken = newAccessToken,
            AccessTokenExpiry = _jwtService.GetAccessTokenExpiry(),
            RefreshToken = newRefreshToken,
            RefreshTokenExpiry = newRefreshExpiry
        });
    }

    // ── Email Confirmation ────────────────────────────────────────────────────

    public async Task<ServiceResult> ConfirmEmailAsync(
        string userId,
        string token,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult.Failure("User not found.");

        if (user.EmailConfirmed)
            return ServiceResult.Failure("Email is already confirmed.");

        // Decode URL-safe Base64 token
        var decodedToken = Encoding.UTF8.GetString(
            WebEncoders.Base64UrlDecode(token));

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return ServiceResult.Failure("Invalid or expired confirmation link.");

        await _emailService.SendWelcomeEmailAsync(user.Email!, GetDisplayName(user), ct);

        _logger.LogInformation("Email confirmed for user: {UserId}", userId);
        return ServiceResult.Success();
    }

    // ── Forgot Password ───────────────────────────────────────────────────────

    public async Task<ServiceResult> ForgotPasswordAsync(
        ForgotPasswordDto dto,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        // Always return success — never reveal whether the email exists
        if (user is null || !user.EmailConfirmed)
            return ServiceResult.Success();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = _config["ClientBaseUrl"];
        var resetLink = $"{baseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(dto.Email)}&token={encodedToken}";

        await _emailService.SendPasswordResetAsync(
            user.Email!, GetDisplayName(user), resetLink, ct);

        _logger.LogInformation("Password reset email sent to: {Email}", dto.Email);
        return ServiceResult.Success();
    }

    // ── Reset Password ────────────────────────────────────────────────────────

    public async Task<ServiceResult> ResetPasswordAsync(
        ResetPasswordDto dto,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return ServiceResult.Failure("Invalid request.");

        var decodedToken = Encoding.UTF8.GetString(
            WebEncoders.Base64UrlDecode(dto.Token));

        var result = await _userManager.ResetPasswordAsync(
            user, decodedToken, dto.NewPassword);

        if (!result.Succeeded)
            return ServiceResult.Failure(
                result.Errors.Select(e => e.Description).ToList());

        // Revoke any existing refresh tokens on password reset (security)
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Password reset successful for: {Email}", dto.Email);
        return ServiceResult.Success();
    }

    // ── Revoke Refresh Token ──────────────────────────────────────────────────

    public async Task<ServiceResult> RevokeRefreshTokenAsync(
        string userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult.Failure("User not found.");

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Refresh token revoked for user: {UserId}", userId);
        return ServiceResult.Success();
    }

    // ── Change Password ───────────────────────────────────────────────────────

    public async Task<ServiceResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult.Failure("User not found.");

        var result = await _userManager.ChangePasswordAsync(
            user, currentPassword, newPassword);

        if (!result.Succeeded)
            return ServiceResult.Failure(
                result.Errors.Select(e => e.Description).ToList());

        // Revoke refresh token so all other sessions are logged out
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        return ServiceResult.Success();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static IEnumerable<Claim> BuildClaims(ApplicationUser user, string role)
        => new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

    private AuthResultDto BuildAuthResult(ApplicationUser user, string role, string fullName)
    {
        // Registration result — returns token but email must be confirmed before login
        var claims = BuildClaims(user, role);
        return new AuthResultDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FullName = fullName,
            Role = role,
            AccessToken = string.Empty,   // not issued until email confirmed
            RefreshToken = string.Empty,
            AccessTokenExpiry = DateTime.UtcNow,
            RefreshTokenExpiry = DateTime.UtcNow
        };
    }

    private async Task<string> GetFullNameAsync(
        string userId, string role, CancellationToken ct)
    {
        if (role == "Student")
        {
            var student = await _uow.Students.GetByUserIdAsync(userId, ct);
            return student?.FullName ?? string.Empty;
        }
        if (role == "Owner")
        {
            var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);
            return owner?.FullName ?? string.Empty;
        }
        return "Admin";
    }

    private async Task<string?> GetProfileImageAsync(
        string userId, string role, CancellationToken ct)
    {
        if (role == "Student")
        {
            var student = await _uow.Students.GetByUserIdAsync(userId, ct);
            return student?.ProfileImageUrl;
        }
        if (role == "Owner")
        {
            var owner = await _uow.Owners.GetByUserIdAsync(userId, ct);
            return owner?.ProfileImageUrl;
        }
        return null;
    }

    private async Task SendConfirmationEmailAsync(
        ApplicationUser user, CancellationToken ct)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var baseUrl = _config["ClientBaseUrl"];
        var link = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

        await _emailService.SendEmailConfirmationAsync(
            user.Email!, GetDisplayName(user), link, ct);
    }

    private static string GetDisplayName(ApplicationUser user)
        => user.UserName ?? user.Email ?? "User";
}