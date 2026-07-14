// Skanly.Infrastructure/ExternalServices/GoogleMaps/GoogleMapsSettings.cs
namespace Skanly.Infrastructure.ExternalServices.GoogleMaps;

public class GoogleMapsSettings
{
    public const string SectionName = "GoogleMaps";

    public string ServerApiKey { get; init; } = string.Empty;
    public string BrowserApiKey { get; init; } = string.Empty;
    public decimal DefaultLatitude { get; init; } = 30.0444m;
    public decimal DefaultLongitude { get; init; } = 31.2357m;
    public int DefaultZoom { get; init; } = 12;
}