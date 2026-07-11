// Skanly.Web/Controllers/AccountController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Auth.DTOs;
using Skanly.Application.Features.Auth.Interfaces;

namespace Skanly.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    // ── Register ──────────────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RegisterStudent()
        => View();
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterStudent(
        RegisterStudentDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.RegisterStudentAsync(dto, ct);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(dto);
        }

        TempData["SuccessMessage"] =
            "Registration successful! Please check your email to confirm your account.";
        return RedirectToAction(nameof(EmailConfirmationSent));
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RegisterOwner()
        => View();
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterOwner(
        RegisterOwnerDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.RegisterOwnerAsync(dto, ct);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(dto);
        }

        TempData["SuccessMessage"] =
            "Registration successful! Please check your email to confirm your account.";
        return RedirectToAction(nameof(EmailConfirmationSent));
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginDto dto,
        string? returnUrl,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.LoginAsync(dto, ct);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(dto);
        }

        var auth = result.Data!;

        // Store JWT in HttpOnly cookie (more secure than localStorage)
        Response.Cookies.Append("skanly_access_token", auth.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = auth.AccessTokenExpiry
        });

        Response.Cookies.Append("skanly_refresh_token", auth.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = auth.RefreshTokenExpiry
        });

        // Redirect based on role
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return auth.Role switch
        {
            "Admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
            "Owner" => RedirectToAction("Index", "Dashboard", new { area = "Owner" }),
            _ => RedirectToAction("Index", "Dashboard", new { area = "Student" })
        };
    }

    // ── Logout ────────────────────────────────────────────────────────────────

#pragma warning disable ASP0026 // [Authorize] overridden by [AllowAnonymous] from farther away
    [HttpPost, ValidateAntiForgeryToken, Authorize]
#pragma warning restore ASP0026 // [Authorize] overridden by [AllowAnonymous] from farther away
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId is not null)
            await _authService.RevokeRefreshTokenAsync(userId, ct);

        Response.Cookies.Delete("skanly_access_token");
        Response.Cookies.Delete("skanly_refresh_token");

        return RedirectToAction(nameof(Login));
    }

    // ── Email Confirmation ────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public IActionResult EmailConfirmationSent() => View();
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(
        string userId,
        string token,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        var result = await _authService.ConfirmEmailAsync(userId, token, ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess
                ? "Email confirmed successfully! You can now log in."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Login));
    }

    // ── Forgot / Reset Password ───────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View();
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);

        await _authService.ForgotPasswordAsync(dto, ct);

        // Always show the same message — never reveal if email exists
        TempData["SuccessMessage"] =
            "If that email is registered, you will receive a password reset link shortly.";
        return RedirectToAction(nameof(Login));
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
        => View(new ResetPasswordDto { Email = email, Token = token });
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.ResetPasswordAsync(dto, ct);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(dto);
        }

        TempData["SuccessMessage"] =
            "Password reset successfully. Please log in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    // ── Refresh Token (AJAX endpoint) ─────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var accessToken = Request.Cookies["skanly_access_token"];
        var refreshToken = Request.Cookies["skanly_refresh_token"];

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await _authService.RefreshTokenAsync(
            new RefreshTokenDto { AccessToken = accessToken, RefreshToken = refreshToken }, ct);

        if (!result.IsSuccess)
        {
            Response.Cookies.Delete("skanly_access_token");
            Response.Cookies.Delete("skanly_refresh_token");
            return Unauthorized();
        }

        var auth = result.Data!;

        Response.Cookies.Append("skanly_access_token", auth.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = auth.AccessTokenExpiry
        });

        Response.Cookies.Append("skanly_refresh_token", auth.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = auth.RefreshTokenExpiry
        });

        return Ok();
    }

    // ── Access Denied ─────────────────────────────────────────────────────────
    
    [AllowAnonymous]    
    [HttpGet]
    public IActionResult AccessDenied() => View();
}