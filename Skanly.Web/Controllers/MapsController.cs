// Skanly.Web/Controllers/MapsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Maps.DTOs;
using Skanly.Application.Features.Maps.Interfaces;
using Skanly.Application.Features.Properties.Interfaces;
using Skanly.Application.Features.Universities.Interfaces;

namespace Skanly.Web.Controllers;


public class MapsController : Controller
{
    private readonly IGoogleMapsService _mapsService;
    private readonly IPropertyService _propertyService;
    private readonly IUniversityService _universityService;
    private readonly IUnitOfWork _uow;

    public MapsController(
        IGoogleMapsService mapsService,
        IPropertyService propertyService,
        IUniversityService universityService,
        IUnitOfWork uow)
    {
        _mapsService = mapsService;
        _propertyService = propertyService;
        _universityService = universityService;
        _uow = uow;
    }

    // ── Full Map Explore Page ─────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Explore(
        int? universityId = null,
        CancellationToken ct = default)
    {
        var universities = await _universityService.GetActiveListAsync(ct);

        ViewBag.BrowserApiKey = _mapsService.GetBrowserApiKey();
        ViewBag.DefaultCenter = _mapsService.GetDefaultCenter();
        ViewBag.Universities = universities.Data;
        ViewBag.UniversityId = universityId;

        return View();
    }

    // ── AJAX: Property pins for the map ───────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> PropertyPins(
        int? universityId = null,
        int? areaId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        // Reuse the search infrastructure from Part 11
        var filter = new Skanly.Application.Features.Properties.DTOs
            .PropertySearchRequestDto
        {
            UniversityId = universityId,
            AreaId = areaId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            PageSize = 200  // max pins on map
        };

        var result = await _propertyService.SearchAsync(filter, null, ct);
        if (!result.IsSuccess)
            return Ok(Array.Empty<PropertyMapDto>());

        var pins = result.Data!.Items.Select(p => new PropertyMapDto
        {
            PropertyId = p.PropertyId,
            Title = p.Title,
            Latitude = 0,   // enriched below
            Longitude = 0,
            PricePerMonth = p.PricePerMonth,
            PropertyTypeDisplay = p.PropertyTypeDisplay,
            AreaNameEn = p.AreaNameEn,
            AverageRating = p.AverageRating,
            PrimaryImageUrl = p.PrimaryImageUrl,
            IsAvailable = true
        }).ToList();

        // Enrich with coordinates from DB
        var enriched = new List<PropertyMapDto>();
        foreach (var pin in pins)
        {
            var property = await _uow.Properties.GetByIdAsync(pin.PropertyId, ct);
            if (property is null)
                continue;

            pin.Latitude = property.Latitude;
            pin.Longitude = property.Longitude;

            enriched.Add(pin);
        }

        return Ok(enriched);
    }

    // ── AJAX: University pins ─────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> UniversityPins(CancellationToken ct)
    {
        var result = await _universityService.GetActiveListAsync(ct);
        if (!result.IsSuccess) return Ok(Array.Empty<object>());

        var pins = result.Data!.Select(u => new
        {
            u.UniversityId,
            u.NameEn,
            u.NameAr,
            u.Latitude,
            u.Longitude,
            u.Address
        });

        return Ok(pins);
    }

    // ── AJAX: Distance between property and university ────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Distance(
        decimal fromLat,
        decimal fromLng,
        decimal toLat,
        decimal toLng,
        CancellationToken ct)
    {
        var origin = LocationDto.From(fromLat, fromLng);
        var dest = LocationDto.From(toLat, toLng);

        var result = await _mapsService.GetDistanceAsync(origin, dest, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : Ok(new { error = result.ErrorMessage });
    }

    // ── AJAX: Geocode address ─────────────────────────────────────────────────

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Geocode(
        string address,
        CancellationToken ct)
    {
        var result = await _mapsService.GeocodeAddressAsync(address, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    // ── AJAX: Reverse geocode ─────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ReverseGeocode(
        decimal lat,
        decimal lng,
        CancellationToken ct)
    {
        var result = await _mapsService.ReverseGeocodeAsync(lat, lng, ct);

        return result.IsSuccess
            ? Ok(new { address = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    // ── AJAX: Nearby transport ────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> NearbyTransport(
        decimal lat,
        decimal lng,
        int radius = 1000,
        CancellationToken ct = default)
    {
        var result = await _mapsService
            .GetNearbyTransportAsync(lat, lng, radius, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : Ok(Array.Empty<NearbyPlaceDto>());
    }

    // ── AJAX: Nearby services ─────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> NearbyServices(
        decimal lat,
        decimal lng,
        int radius = 1500,
        CancellationToken ct = default)
    {
        var result = await _mapsService
            .GetNearbyServicesAsync(lat, lng, radius, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : Ok(Array.Empty<NearbyPlaceDto>());
    }

    // ── AJAX: Directions ──────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Directions(
        decimal fromLat,
        decimal fromLng,
        decimal toLat,
        decimal toLng,
        string mode = "DRIVING",
        CancellationToken ct = default)
    {
        var validModes = new[] { "DRIVING", "WALKING", "TRANSIT" };
        if (!validModes.Contains(mode.ToUpper()))
            mode = "DRIVING";

        var origin = LocationDto.From(fromLat, fromLng);
        var dest = LocationDto.From(toLat, toLng);

        var result = await _mapsService
            .GetDirectionsAsync(origin, dest, mode.ToUpper(), ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : Ok(new DirectionsDto { HasRoute = false });
    }
}