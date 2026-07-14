// Skanly.Application/Features/Recommendations/Services/StudentPreferenceAnalyzer.cs
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Recommendations.DTOs;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Recommendations.Services;

/// <summary>
/// Builds a StudentPreferenceProfileDto from three behavioral signals:
///   1. Favorites — what the student actively saved
///   2. Search history — what criteria they filtered by
///   3. Booking history — what they actually requested/confirmed
///
/// Each signal is weighted separately to reflect its predictive strength.
/// Booking history has the highest signal quality (strongest intent).
/// </summary>
public class StudentPreferenceAnalyzer
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StudentPreferenceAnalyzer> _logger;

    private static readonly TimeSpan SearchHistoryWindow = TimeSpan.FromDays(60);

    public StudentPreferenceAnalyzer(
        IUnitOfWork uow,
        ILogger<StudentPreferenceAnalyzer> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<StudentPreferenceProfileDto> BuildProfileAsync(
        string studentId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Building preference profile for student {StudentId}", studentId);

        // ── Load student base data ─────────────────────────────────────────────
        var student = await _uow.Students
            .GetWithUniversityAsync(studentId, ct);

        decimal? uniLat = null, uniLng = null;
        if (student?.UniversityId.HasValue == true)
        {
            var uni = await _uow.Universities
                .GetByIdAsync(student.UniversityId.Value, ct);
            uniLat = uni?.Latitude;
            uniLng = uni?.Longitude;
        }

        // ── Load behavioral signals in parallel ────────────────────────────────
        var favoritesTask = AnalyzeFavoritesAsync(studentId, ct);
        var searchHistoryTask = AnalyzeSearchHistoryAsync(studentId, ct);
        var bookingHistoryTask = AnalyzeBookingHistoryAsync(studentId, ct);

        await Task.WhenAll(favoritesTask, searchHistoryTask, bookingHistoryTask);

        var favorites = await favoritesTask;
        var searchHistory = await searchHistoryTask;
        var bookingHistory = await bookingHistoryTask;

        // ── Merge signals into unified profile ─────────────────────────────────
        var mergedProfile = MergeSignals(
            studentId,
            student,
            favorites,
            searchHistory,
            bookingHistory,
            uniLat,
            uniLng);

        _logger.LogInformation(
            "Profile built for {StudentId}. Confidence={Confidence:F1} " +
            "Favorites={Fav} Searches={Search} Bookings={Book}",
            studentId,
            mergedProfile.ProfileConfidence,
            favorites.TotalFavorites,
            searchHistory.TotalSearches,
            bookingHistory.TotalBookings);

        return mergedProfile;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SIGNAL 1 — FAVORITES ANALYSIS
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<FavoritesSignal> AnalyzeFavoritesAsync(
        string studentId,
        CancellationToken ct)
    {
        var favorites = await _uow.Favorites.GetByStudentIdAsync(
            studentId, ct);

        if (!favorites.Any())
            return new FavoritesSignal();

        // Load the full property details for each favorite
        var favProperties = new List<Property>();
        foreach (var fav in favorites)
        {
            var prop = await _uow.Properties
                .GetDetailAsync(fav.PropertyId, ct);
            if (prop is not null && !prop.IsDeleted)
                favProperties.Add(prop);
        }

        if (!favProperties.Any())
            return new FavoritesSignal();

        // Extract price signal
        var prices = favProperties.Select(p => p.PricePerMonth).ToList();
        prices.Sort();
        var medianPrice = prices[prices.Count / 2];

        // Extract area preferences (ordered by frequency)
        var areaCounts = favProperties
            .GroupBy(p => p.AreaId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        // Extract property type preferences
        var typeCounts = favProperties
            .GroupBy(p => p.PropertyType)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        // Extract amenity preferences
        var amenityFrequency = favProperties
            .SelectMany(p => p.PropertyAmenities.Select(pa => pa.AmenityId))
            .GroupBy(id => id)
            .Where(g => g.Count() >= Math.Max(1, favProperties.Count / 3))
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(10)
            .ToList();

        // Gender policy
    

        return new FavoritesSignal
        {
            MedianPrice = medianPrice,
            PreferredAreaIds = areaCounts,
            PreferredTypes = typeCounts,
            PreferredAmenityIds = amenityFrequency,
            TotalFavorites = favorites.Count
        };
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SIGNAL 2 — SEARCH HISTORY ANALYSIS
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<SearchHistorySignal> AnalyzeSearchHistoryAsync(
        string studentId,
        CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - SearchHistoryWindow;

        var searches = await _uow.Repository<StudentSearchHistory>()
            .GetAllAsync(
                s => s.StudentId == studentId &&
                     s.SearchedAt >= cutoff, ct);

        if (!searches.Any())
            return new SearchHistorySignal();

        // Price range from search filters
        var minPrices = searches
            .Where(s => s.MinPrice.HasValue)
            .Select(s => s.MinPrice!.Value)
            .ToList();

        var maxPrices = searches
            .Where(s => s.MaxPrice.HasValue)
            .Select(s => s.MaxPrice!.Value)
            .ToList();

        // Median min/max price
        decimal? medianMinPrice = null;
        decimal? medianMaxPrice = null;

        if (minPrices.Any())
        {
            minPrices.Sort();
            medianMinPrice = minPrices[minPrices.Count / 2];
        }

        if (maxPrices.Any())
        {
            maxPrices.Sort();
            medianMaxPrice = maxPrices[maxPrices.Count / 2];
        }

        // Most searched areas
        var preferredAreaIds = searches
            .Where(s => s.AreaId.HasValue)
            .GroupBy(s => s.AreaId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        // Most searched property types
        var preferredTypes = searches
            .Where(s => s.PropertyType.HasValue)
            .GroupBy(s => s.PropertyType!.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        // Most searched universities
        var preferredUniversityIds = searches
            .Where(s => s.UniversityId.HasValue)
            .GroupBy(s => s.UniversityId!.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        return new SearchHistorySignal
        {
            MedianMinPrice = medianMinPrice,
            MedianMaxPrice = medianMaxPrice,
            PreferredAreaIds = preferredAreaIds,
            PreferredTypes = preferredTypes,
            PreferredUniversityIds = preferredUniversityIds,
            TotalSearches = searches.Count
        };
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SIGNAL 3 — BOOKING HISTORY ANALYSIS
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<BookingHistorySignal> AnalyzeBookingHistoryAsync(
        string studentId,
        CancellationToken ct)
    {
        var (bookings, _) = await _uow.Bookings.GetByStudentIdAsync(
    studentId,
    1,
    int.MaxValue,
    null,
    ct);

        if (!bookings.Any())
            return new BookingHistorySignal();

        // Load property details for confirmed/accepted bookings
        // These are the strongest signal — real booking intent
        var confirmedBookings = bookings
            .Where(b => b.Status == BookingStatus.Confirmed ||
                        b.Status == BookingStatus.Accepted)
            .ToList();

        var allBookingProperties = new List<Property>();
        foreach (var booking in bookings)
        {
            var prop = await _uow.Properties
                .GetDetailAsync(booking.PropertyId, ct);
            if (prop is not null)
                allBookingProperties.Add(prop);
        }

        var confirmedProperties = allBookingProperties
            .Where(p => confirmedBookings.Any(b => b.PropertyId == p.Id))
            .ToList();

        // Price signal from booked properties
        var bookedPrices = allBookingProperties
            .Select(p => p.PricePerMonth)
            .ToList();

        decimal? medianBookedPrice = null;
        if (bookedPrices.Any())
        {
            bookedPrices.Sort();
            medianBookedPrice = bookedPrices[bookedPrices.Count / 2];
        }

        // Area preferences from bookings
        var preferredAreaIds = allBookingProperties
            .GroupBy(p => p.AreaId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(5)
            .ToList();

        // Amenities from confirmed properties (highest quality signal)
        var preferredAmenityIds = confirmedProperties
           .SelectMany(p => p.PropertyAmenities.Select(pa => pa.AmenityId))
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(10)
            .ToList();

        // Property types
        var preferredTypes = allBookingProperties
            .GroupBy(p => p.PropertyType)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        return new BookingHistorySignal
        {
            MedianBookedPrice = medianBookedPrice,
            PreferredAreaIds = preferredAreaIds,
            PreferredTypes = preferredTypes,
            PreferredAmenityIds = preferredAmenityIds,
            TotalBookings = bookings.Count,
            ConfirmedBookings = confirmedBookings.Count
        };
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SIGNAL MERGER
    // ══════════════════════════════════════════════════════════════════════════

    private static StudentPreferenceProfileDto MergeSignals(
        string studentId,
        Student? student,
        FavoritesSignal favorites,
        SearchHistorySignal search,
        BookingHistorySignal bookings,
        decimal? uniLat,
        decimal? uniLng)
    {
        // ── Price inference ────────────────────────────────────────────────────
        // Weighted blend: bookings > favorites > search history
        var prices = new List<(decimal Price, double Weight)>();

        if (bookings.MedianBookedPrice.HasValue)
            prices.Add((bookings.MedianBookedPrice.Value, 0.50));
        if (favorites.MedianPrice.HasValue)
            prices.Add((favorites.MedianPrice.Value, 0.35));
        if (search.MedianMaxPrice.HasValue)
            prices.Add((search.MedianMaxPrice.Value, 0.15));

        decimal? inferredPrice = null;
        if (prices.Any())
        {
            var totalWeight = prices.Sum(p => p.Weight);
            inferredPrice = prices.Sum(p => p.Price * (decimal)p.Weight) /
                                (decimal)totalWeight;
        }

        decimal? inferredMin = inferredPrice.HasValue
            ? inferredPrice.Value * 0.75m : search.MedianMinPrice;
        decimal? inferredMax = inferredPrice.HasValue
            ? inferredPrice.Value * 1.30m : search.MedianMaxPrice;

        // ── Area preferences ───────────────────────────────────────────────────
        // Merge area IDs across signals — rank by weighted frequency
        var areaScores = new Dictionary<int, double>();

        void AddAreaIds(IReadOnlyList<int> ids, double weight)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                var score = weight * (ids.Count - i) / ids.Count;
                areaScores[ids[i]] = areaScores.GetValueOrDefault(ids[i]) + score;
            }
        }

        AddAreaIds(bookings.PreferredAreaIds, 0.50);
        AddAreaIds(favorites.PreferredAreaIds, 0.35);
        AddAreaIds(search.PreferredAreaIds, 0.15);

        var mergedAreaIds = areaScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .Take(5)
            .ToList();

        // ── Property type preferences ──────────────────────────────────────────
        var typeScores = new Dictionary<PropertyType, double>();

        void AddTypes(IReadOnlyList<PropertyType> types, double weight)
        {
            for (int i = 0; i < types.Count; i++)
            {
                var score = weight * (types.Count - i) / types.Count;
                typeScores[types[i]] =
                    typeScores.GetValueOrDefault(types[i]) + score;
            }
        }

        AddTypes(bookings.PreferredTypes, 0.50);
        AddTypes(favorites.PreferredTypes, 0.35);
        AddTypes(search.PreferredTypes, 0.15);

        var mergedTypes = typeScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        // ── Amenity preferences ────────────────────────────────────────────────
        var amenityScores = new Dictionary<int, double>();

        void AddAmenities(IReadOnlyList<int> ids, double weight)
        {
            foreach (var id in ids)
                amenityScores[id] =
                    amenityScores.GetValueOrDefault(id) + weight;
        }

        // Booking amenities: highest weight
        AddAmenities(bookings.PreferredAmenityIds, 0.50);
        // Favorite amenities: strong signal
        AddAmenities(favorites.PreferredAmenityIds, 0.35);

        var mergedAmenityIds = amenityScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .Take(10)
            .ToList();

        return new StudentPreferenceProfileDto
        {
            StudentId = studentId,
            StudentFullName = student?.FullName,
            UniversityId = student?.UniversityId,
            UniversityLatitude = uniLat,
            UniversityLongitude = uniLng,

            FavoriteMedianPrice = favorites.MedianPrice,
            SearchMedianMinPrice = search.MedianMinPrice,
            SearchMedianMaxPrice = search.MedianMaxPrice,
            BookingMedianPrice = bookings.MedianBookedPrice,
            InferredMinPrice = inferredMin,
            InferredMaxPrice = inferredMax,

            PreferredAreaIds = mergedAreaIds,
            PreferredPropertyTypes = mergedTypes,
            PreferredAmenityIds = mergedAmenityIds,

            TotalFavorites = favorites.TotalFavorites,
            TotalSearches = search.TotalSearches,
            TotalBookings = bookings.TotalBookings,
            TotalViewedProperties = favorites.TotalFavorites +
                                     bookings.TotalBookings,

            BuiltAt = DateTime.UtcNow
        };
    }

    // ── Private Signal Records ────────────────────────────────────────────────

    private record FavoritesSignal
    {
        public decimal? MedianPrice { get; init; }
        public IReadOnlyList<int> PreferredAreaIds { get; init; } =
            new List<int>();
        public IReadOnlyList<PropertyType> PreferredTypes { get; init; } =
            new List<PropertyType>();
        public IReadOnlyList<int> PreferredAmenityIds { get; init; } =
            new List<int>();
        public int TotalFavorites { get; init; }
    }

    private record SearchHistorySignal
    {
        public decimal? MedianMinPrice { get; init; }
        public decimal? MedianMaxPrice { get; init; }
        public IReadOnlyList<int> PreferredAreaIds { get; init; } =
            new List<int>();
        public IReadOnlyList<PropertyType> PreferredTypes { get; init; } =
            new List<PropertyType>();
        public IReadOnlyList<int> PreferredUniversityIds { get; init; } =
            new List<int>();
        public int TotalSearches { get; init; }
    }

    private record BookingHistorySignal
    {
        public decimal? MedianBookedPrice { get; init; }
        public IReadOnlyList<int> PreferredAreaIds { get; init; } =
            new List<int>();
        public IReadOnlyList<PropertyType> PreferredTypes { get; init; } =
            new List<PropertyType>();
        public IReadOnlyList<int> PreferredAmenityIds { get; init; } =
            new List<int>();
        public int TotalBookings { get; init; }
        public int ConfirmedBookings { get; init; }
    }
}