// Skanly.Application/Features/Maps/Interfaces/IGoogleMapsService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Maps.DTOs;

namespace Skanly.Application.Features.Maps.Interfaces;

public interface IGoogleMapsService
{
    // ── Distance ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates driving, walking, and transit distances between
    /// a property and a university. Called when a property is displayed.
    /// Results should be cached — they rarely change.
    /// </summary>
    Task<ServiceResult<DistanceResultDto>> GetDistanceAsync(
        LocationDto origin,
        LocationDto destination,
        CancellationToken ct = default);

    /// <summary>
    /// Straight-line (Haversine) distance in km.
    /// Instant — no API call required.
    /// </summary>
    double GetStraightLineDistanceKm(
        decimal lat1, decimal lng1,
        decimal lat2, decimal lng2);

    // ── Geocoding ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a free-text address to coordinates.
    /// Used in the owner property form when the owner types an address.
    /// </summary>
    Task<ServiceResult<LocationDto>> GeocodeAddressAsync(
        string address,
        CancellationToken ct = default);

    /// <summary>
    /// Converts coordinates to a human-readable address (reverse geocode).
    /// </summary>
    Task<ServiceResult<string>> ReverseGeocodeAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken ct = default);

    // ── Nearby Places ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns nearby transportation stops (metro, bus, train)
    /// within the given radius.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<NearbyPlaceDto>>> GetNearbyTransportAsync(
        decimal latitude,
        decimal longitude,
        int radiusMeters = 1000,
        CancellationToken ct = default);

    /// <summary>
    /// Returns nearby services (supermarket, hospital, pharmacy,
    /// restaurant, cafe, bank, mosque) within the given radius.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<NearbyPlaceDto>>> GetNearbyServicesAsync(
        decimal latitude,
        decimal longitude,
        int radiusMeters = 1500,
        CancellationToken ct = default);

    // ── Directions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns route directions between two points.
    /// travelMode: DRIVING | WALKING | TRANSIT
    /// </summary>
    Task<ServiceResult<DirectionsDto>> GetDirectionsAsync(
        LocationDto origin,
        LocationDto destination,
        string travelMode = "DRIVING",
        CancellationToken ct = default);

    // ── Browser API Key (safe to expose — domain-restricted) ─────────────────

    string GetBrowserApiKey();

    LocationDto GetDefaultCenter();
}