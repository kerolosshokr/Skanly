// Skanly.Web/Controllers/CultureController.cs
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Skanly.Web.Controllers;

public class CultureController : Controller
{
    private const string CookieName = ".Skanly.Culture";

    // ── Set Language ──────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [Route("/Culture/Set")]
    public IActionResult Set(string culture, string returnUrl = "/")
    {
        var allowedCultures = new[] { "en", "ar" };

        if (!allowedCultures.Contains(culture))
            culture = "en";

        Response.Cookies.Append(
            CookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture, culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                HttpOnly = false   // JS can read for RTL toggle
            });

        // Validate returnUrl to prevent open redirect
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return LocalRedirect(returnUrl);
    }

    // ── AJAX: Get current culture ──────────────────────────────────────────────

    [HttpGet, Route("/Culture/Current")]
    public IActionResult Current()
    {
        var culture = System.Globalization.CultureInfo
            .CurrentUICulture.TwoLetterISOLanguageName;

        return Ok(new
        {
            culture,
            isArabic = culture == "ar",
            isRtl = culture == "ar",
            dir = culture == "ar" ? "rtl" : "ltr"
        });
    }
}