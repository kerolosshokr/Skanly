// Skanly.Application/Features/Localization/Services/LocalizationService.cs
using Microsoft.AspNetCore.Http;
using Skanly.Application.Common.Interfaces;
using System.Globalization;

namespace Skanly.Application.Features.Localization.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentCulture
        => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public bool IsArabic => CurrentCulture == "ar";

    public bool IsRtl => IsArabic;

    public string Resolve(string? nameEn, string? nameAr)
    {
        if (IsArabic && !string.IsNullOrWhiteSpace(nameAr))
            return nameAr;

        return nameEn ?? string.Empty;
    }

    public string FormatCurrency(decimal amount)
    {
        // Egyptian Pound — EGP prefix, period as decimal separator
        return IsArabic
            ? $"{amount:N0} ج.م"
            : $"EGP {amount:N0}";
    }

    public string FormatDate(DateTime date, string format = "D")
    {
        var culture = IsArabic
            ? new CultureInfo("ar-EG")
            : CultureInfo.InvariantCulture;

        return format == "D"
            ? date.ToString("MMMM dd, yyyy", culture)
            : date.ToString(format, culture);
    }

    public string FormatDate(DateOnly date, string format = "D")
        => FormatDate(date.ToDateTime(TimeOnly.MinValue), format);
}