// Skanly.Web/Controllers/SettingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Skanly.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private const string ThemeCookieName = "skanly-theme";

    // ── Persist theme preference server-side (optional backup to localStorage) ──

    [HttpPost, ValidateAntiForgeryToken]
    [Route("/Settings/Theme")]
    public IActionResult SetTheme(string preference)
    {
        var allowed = new[] { "light", "dark", "system" };
        if (!allowed.Contains(preference))
            preference = "system";

        Response.Cookies.Append(
            ThemeCookieName,
            preference,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                HttpOnly = false   // JS must read this for SSR consistency
            });

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(new { preference });

        return Ok();
    }

    [HttpGet, Route("/Settings/Theme")]
    public IActionResult GetTheme()
    {
        var pref = Request.Cookies[ThemeCookieName] ?? "system";
        return Ok(new { preference = pref });
    }
}