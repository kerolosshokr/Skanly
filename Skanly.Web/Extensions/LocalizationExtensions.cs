// Extension method for views — resolves bilingual text
// Skanly.Web/Extensions/LocalizationExtensions.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace Skanly.Web.Extensions;

public static class LocalizationExtensions
{
    public static bool IsArabicCulture(this IHtmlHelper _)
        => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    /// <summary>
    /// Returns Arabic value if culture is Arabic and value is not empty;
    /// otherwise returns English value.
    /// </summary>
    public static string Resolve(
        this IHtmlHelper _,
        string? nameEn,
        string? nameAr)
    {
        var isAr = CultureInfo.CurrentUICulture
            .TwoLetterISOLanguageName == "ar";

        return isAr && !string.IsNullOrWhiteSpace(nameAr)
            ? nameAr
            : nameEn ?? string.Empty;
    }
}