// Skanly.Infrastructure/ExternalServices/GoogleMaps/GoogleMapsService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Maps.DTOs;
using Skanly.Application.Features.Maps.Interfaces;
using System.Text.Json;

namespace Skanly.Infrastructure.ExternalServices.GoogleMaps;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _http;
    private readonly GoogleMapsSettings _settings;
    private readonly ILogger<GoogleMapsService> _logger;

    // Google Maps REST API base
    private const string MapsBase = "https://maps.googleapis.com/maps/api";

    public GoogleMapsService(
        HttpClient http,
        IOptions<GoogleMapsSettings> settings,
        ILogger<GoogleMapsService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    // ── GetDistanceAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<DistanceResultDto>> GetDistanceAsync(
        LocationDto origin,
        LocationDto destination,
        CancellationToken ct = default)
    {
        try
        {
            var orig = $"{origin.Latitude},{origin.Longitude}";
            var dest = $"{destination.Latitude},{destination.Longitude}";
            var key = _settings.ServerApiKey;

            // Run driving + walking in parallel; transit separately (Egypt coverage varies)
            var drivingTask = FetchDistanceMatrixAsync(
                orig, dest, "driving", key, ct);
            var walkingTask = FetchDistanceMatrixAsync(
                orig, dest, "walking", key, ct);
            var transitTask = FetchDistanceMatrixAsync(
                orig, dest, "transit", key, ct);

            await Task.WhenAll(drivingTask, walkingTask, transitTask);

            var driving = await drivingTask;
            var walking = await walkingTask;
            var transit = await transitTask;

            var straightLine = GetStraightLineDistanceKm(
                origin.Latitude, origin.Longitude,
                destination.Latitude, destination.Longitude);

            var result = new DistanceResultDto
            {
                OriginAddress = origin.Address ?? orig,
                DestinationAddress = destination.Address ?? dest,

                DrivingDistanceMeters = driving?.DistanceValue,
                DrivingDistanceText = driving?.DistanceText,
                DrivingDurationText = driving?.DurationText,

                WalkingDistanceMeters = walking?.DistanceValue,
                WalkingDistanceText = walking?.DistanceText,
                WalkingDurationText = walking?.DurationText,

                TransitDistanceMeters = transit?.DistanceValue,
                TransitDistanceText = transit?.DistanceText,
                TransitDurationText = transit?.DurationText,

                StraightLineDistanceKm = Math.Round(straightLine, 2)
            };

            return ServiceResult<DistanceResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Distance Matrix API error");
            return ServiceResult<DistanceResultDto>.Failure(
                "Could not calculate distance. Please try again.");
        }
    }

    // ── GetStraightLineDistanceKm ─────────────────────────────────────────────

    public double GetStraightLineDistanceKm(
        decimal lat1, decimal lng1,
        decimal lat2, decimal lng2)
    {
        const double R = 6371.0;  // Earth radius in km

        var dLat = ToRadians((double)(lat2 - lat1));
        var dLng = ToRadians((double)(lng2 - lng1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians((double)lat1))
              * Math.Cos(ToRadians((double)lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    // ── GeocodeAddressAsync ───────────────────────────────────────────────────

    public async Task<ServiceResult<LocationDto>> GeocodeAddressAsync(
        string address,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{MapsBase}/geocode/json" +
                      $"?address={Uri.EscapeDataString(address + ", Egypt")}" +
                      $"&key={_settings.ServerApiKey}";

            var response = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "OK")
                return ServiceResult<LocationDto>.Failure(
                    "Address not found. Please enter a more specific address.");

            var location = root
                .GetProperty("results")[0]
                .GetProperty("geometry")
                .GetProperty("location");

            var formattedAddress = root
                .GetProperty("results")[0]
                .GetProperty("formatted_address")
                .GetString() ?? address;

            return ServiceResult<LocationDto>.Success(new LocationDto
            {
                Latitude = (decimal)location.GetProperty("lat").GetDouble(),
                Longitude = (decimal)location.GetProperty("lng").GetDouble(),
                Address = formattedAddress
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geocoding error for address: {Address}", address);
            return ServiceResult<LocationDto>.Failure("Geocoding failed.");
        }
    }

    // ── ReverseGeocodeAsync ───────────────────────────────────────────────────

    public async Task<ServiceResult<string>> ReverseGeocodeAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{MapsBase}/geocode/json" +
                      $"?latlng={latitude},{longitude}" +
                      $"&key={_settings.ServerApiKey}";

            var response = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "OK")
                return ServiceResult<string>.Failure("Address not found.");

            var address = root
                .GetProperty("results")[0]
                .GetProperty("formatted_address")
                .GetString() ?? $"{latitude},{longitude}";

            return ServiceResult<string>.Success(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse geocoding error");
            return ServiceResult<string>.Failure("Could not resolve address.");
        }
    }

    // ── GetNearbyTransportAsync ───────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<NearbyPlaceDto>>> GetNearbyTransportAsync(
        decimal latitude,
        decimal longitude,
        int radiusMeters = 1000,
        CancellationToken ct = default)
    {
        var types = new[] { "subway_station", "bus_station",
                            "train_station",  "transit_station" };

        var results = new List<NearbyPlaceDto>();
        foreach (var type in types)
        {
            var places = await FetchNearbyPlacesAsync(
                latitude, longitude, radiusMeters, type, ct);
            results.AddRange(places);
        }

        var ordered = results
            .OrderBy(p => p.DistanceMeters)
            .Take(10)
            .ToList();

        return ServiceResult<IReadOnlyList<NearbyPlaceDto>>.Success(ordered);
    }

    // ── GetNearbyServicesAsync ────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<NearbyPlaceDto>>> GetNearbyServicesAsync(
        decimal latitude,
        decimal longitude,
        int radiusMeters = 1500,
        CancellationToken ct = default)
    {
        var types = new[]
        {
            "supermarket", "hospital", "pharmacy",
            "restaurant",  "cafe",     "bank",
            "atm",         "gym",      "mosque"
        };

        var results = new List<NearbyPlaceDto>();
        foreach (var type in types)
        {
            var places = await FetchNearbyPlacesAsync(
                latitude, longitude, radiusMeters, type, ct);
            // Take top 2 per category
            results.AddRange(places.Take(2));
        }

        var ordered = results
            .OrderBy(p => p.DistanceMeters)
            .Take(20)
            .ToList();

        return ServiceResult<IReadOnlyList<NearbyPlaceDto>>.Success(ordered);
    }

    // ── GetDirectionsAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<DirectionsDto>> GetDirectionsAsync(
        LocationDto origin,
        LocationDto destination,
        string travelMode = "DRIVING",
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{MapsBase}/directions/json" +
                      $"?origin={origin.Latitude},{origin.Longitude}" +
                      $"&destination={destination.Latitude},{destination.Longitude}" +
                      $"&mode={travelMode.ToLower()}" +
                      $"&key={_settings.ServerApiKey}";

            var response = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "OK")
                return ServiceResult<DirectionsDto>.Success(
                    new DirectionsDto { HasRoute = false });

            var route = root.GetProperty("routes")[0];
            var leg = route.GetProperty("legs")[0];

            // Encoded polyline for drawing on map
            var polyline = route
                .GetProperty("overview_polyline")
                .GetProperty("points")
                .GetString();

            var totalDist = leg.GetProperty("distance")
                               .GetProperty("text").GetString();
            var totalDur = leg.GetProperty("duration")
                               .GetProperty("text").GetString();

            // Parse steps
            var steps = new List<DirectionStepDto>();
            foreach (var step in leg.GetProperty("steps").EnumerateArray())
            {
                var instruction = step
                    .GetProperty("html_instructions")
                    .GetString() ?? string.Empty;

                // Strip HTML tags from instruction
                instruction = System.Text.RegularExpressions.Regex
                    .Replace(instruction, "<.*?>", " ")
                    .Trim();

                steps.Add(new DirectionStepDto
                {
                    Instruction = instruction,
                    Distance = step.GetProperty("distance")
                                      .GetProperty("text").GetString() ?? "",
                    Duration = step.GetProperty("duration")
                                      .GetProperty("text").GetString() ?? "",
                    TravelMode = step.TryGetProperty("travel_mode",
                                      out var mode)
                                      ? mode.GetString() ?? travelMode
                                      : travelMode
                });
            }

            return ServiceResult<DirectionsDto>.Success(new DirectionsDto
            {
                HasRoute = true,
                EncodedPolyline = polyline,
                TotalDistance = totalDist,
                TotalDuration = totalDur,
                TravelMode = travelMode,
                Steps = steps
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Directions API error");
            return ServiceResult<DirectionsDto>.Success(
                new DirectionsDto { HasRoute = false });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public string GetBrowserApiKey() => _settings.BrowserApiKey;

    public LocationDto GetDefaultCenter() => new()
    {
        Latitude = _settings.DefaultLatitude,
        Longitude = _settings.DefaultLongitude,
        Label = "Cairo"
    };

    // ── Private Helpers ───────────────────────────────────────────────────────

    private record MatrixResult(
        double? DistanceValue,
        string? DistanceText,
        string? DurationText);

    private async Task<MatrixResult?> FetchDistanceMatrixAsync(
        string origin,
        string destination,
        string mode,
        string key,
        CancellationToken ct)
    {
        try
        {
            var url = $"{MapsBase}/distancematrix/json" +
                      $"?origins={origin}" +
                      $"&destinations={destination}" +
                      $"&mode={mode}" +
                      $"&key={key}";

            var response = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "OK")
                return null;

            var element = root
                .GetProperty("rows")[0]
                .GetProperty("elements")[0];

            if (element.GetProperty("status").GetString() != "OK")
                return null;

            return new MatrixResult(
                element.GetProperty("distance").GetProperty("value").GetDouble(),
                element.GetProperty("distance").GetProperty("text").GetString(),
                element.GetProperty("duration").GetProperty("text").GetString());
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<NearbyPlaceDto>> FetchNearbyPlacesAsync(
        decimal latitude,
        decimal longitude,
        int radiusMeters,
        string type,
        CancellationToken ct)
    {
        try
        {
            var url = $"{MapsBase}/place/nearbysearch/json" +
                      $"?location={latitude},{longitude}" +
                      $"&radius={radiusMeters}" +
                      $"&type={type}" +
                      $"&key={_settings.ServerApiKey}";

            var response = await _http.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() is not "OK" and not "ZERO_RESULTS")
                return Array.Empty<NearbyPlaceDto>();

            var results = new List<NearbyPlaceDto>();

            foreach (var place in root.GetProperty("results").EnumerateArray())
            {
                var loc = place.GetProperty("geometry").GetProperty("location");
                var placeLat = (decimal)loc.GetProperty("lat").GetDouble();
                var placeLng = (decimal)loc.GetProperty("lng").GetDouble();

                var distance = GetStraightLineDistanceKm(
                    latitude, longitude, placeLat, placeLng) * 1000;

                double? rating = null;
                if (place.TryGetProperty("rating", out var ratingEl))
                    rating = ratingEl.GetDouble();

                bool isOpen = false;
                if (place.TryGetProperty("opening_hours", out var hours) &&
                    hours.TryGetProperty("open_now", out var openNow))
                    isOpen = openNow.GetBoolean();

                results.Add(new NearbyPlaceDto
                {
                    PlaceId = place.GetProperty("place_id").GetString() ?? "",
                    Name = place.GetProperty("name").GetString() ?? "",
                    Vicinity = place.TryGetProperty("vicinity", out var vic)
                                        ? vic.GetString()
                                        : null,
                    PlaceType = type,
                    Icon = place.TryGetProperty("icon", out var icon)
                                        ? icon.GetString() ?? ""
                                        : "",
                    Latitude = placeLat,
                    Longitude = placeLng,
                    DistanceMeters = Math.Round(distance),
                    Rating = rating,
                    IsOpen = isOpen
                });
            }

            return results
                .OrderBy(p => p.DistanceMeters)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Nearby search failed for type={Type}", type);
            return Array.Empty<NearbyPlaceDto>();
        }
    }

    private static double ToRadians(double degrees)
        => degrees * Math.PI / 180.0;
}