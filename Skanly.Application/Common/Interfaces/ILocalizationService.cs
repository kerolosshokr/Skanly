// Skanly.Application/Common/Interfaces/ILocalizationService.cs
namespace Skanly.Application.Common.Interfaces;

/// <summary>
/// Provides culture-aware string resolution for entities
/// that have bilingual fields (NameEn / NameAr).
/// Application services use this to pick the correct display name.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Current UI culture code — "en" or "ar".</summary>
    string CurrentCulture { get; }

    bool IsArabic { get; }
    bool IsRtl { get; }

    /// <summary>
    /// Returns the Arabic value when Arabic is active,
    /// falling back to English if the Arabic value is null/empty.
    /// </summary>
    string Resolve(string? nameEn, string? nameAr);

    /// <summary>Formats a decimal as EGP currency.</summary>
    string FormatCurrency(decimal amount);

    /// <summary>Formats a date in the current culture's convention.</summary>
    string FormatDate(DateTime date, string format = "D");

    /// <summary>Formats a date in the current culture's convention.</summary>
    string FormatDate(DateOnly date, string format = "D");
}