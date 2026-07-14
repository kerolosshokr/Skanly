// Skanly.Infrastructure/Pdf/ContractFonts.cs
using QuestPDF.Infrastructure;

namespace Skanly.Infrastructure.Pdf;

/// <summary>
/// Font registration for QuestPDF.
/// Arabic text requires a font that supports the Arabic Unicode range.
/// </summary>
public static class ContractFonts
{
    public static void Register()
    {
        // Register fonts once at application startup
        // Cairo font supports Arabic + Latin — download from Google Fonts
        // Place font files in wwwroot/fonts/Cairo-*.ttf

        // Fallback: use system fonts if custom fonts not available
        QuestPDF.Settings.License = LicenseType.Community;
    }
}