// Skanly.Application/Features/Recommendations/Services/RecommendationService.cs
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Maps.Interfaces;
using Skanly.Application.Features.Recommendations.DTOs;
using Skanly.Application.Features.Recommendations.Interfaces;
using Skanly.Application.Features.Recommendations.Scoring;
using Skanly.Domain.Entities;

namespace Skanly.Application.Features.Recommendations.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _uow;
    private readonly StudentPreferenceAnalyzer _analyzer;
    private readonly IClaudeRecommendationClient _claudeClient;
    private readonly IGoogleMapsService _mapsService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RecommendationService> _logger;

    private const int DefaultMaxResults = 10;
    private const double MinMatchScore = 20.0;
    private const int CacheMinutes = 30;
    private const string CacheKeyPrefix = "recommendations:";

    public RecommendationService(
        IUnitOfWork uow,
        StudentPreferenceAnalyzer analyzer,
        IClaudeRecommendationClient claudeClient,
        IGoogleMapsService mapsService,
        IMemoryCache cache,
        ILogger<RecommendationService> logger)
    {
        _uow = uow;
        _analyzer = analyzer;
        _claudeClient = claudeClient;
        _mapsService = mapsService;
        _cache = cache;
        _logger = logger;
    }

    // ── GetRecommendationsAsync ───────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<RecommendationDto>>>
        GetRecommendationsAsync(
            string studentId,
            RecommendationRequestDto? request = null,
            CancellationToken ct = default)
    {
        var maxResults = request?.MaxResults ?? DefaultMaxResults;
        var cacheKey = $"{CacheKeyPrefix}{studentId}:{maxResults}" +
                         $":{request?.MaxPrice}:{request?.AreaId}";

        // Return cached results unless a force-refresh is requested
        if (request?.ForceRefresh != true &&
            _cache.TryGetValue(cacheKey,
                out IReadOnlyList<RecommendationDto>? cached))
        {
            _logger.LogDebug(
                "Returning cached recommendations for {StudentId}", studentId);
            return ServiceResult<IReadOnlyList<RecommendationDto>>
                .Success(cached!);
        }

        // ── Step 1: Build student preference profile ───────────────────────────
        var profile = await _analyzer.BuildProfileAsync(studentId, ct);

        // ── Step 2: Load candidate properties ─────────────────────────────────
        var candidates = await LoadCandidatePropertiesAsync(
            studentId, profile, request, ct);

        if (!candidates.Any())
        {
            // Cold start — student has no behavioral data yet
            // Fall back to top-rated properties near their university
            candidates = await LoadFallbackPropertiesAsync(
                studentId, profile, ct);
        }

        // ── Step 3: Score all candidates ──────────────────────────────────────
        var scores = new List<(Property Property, PropertyScoreDto Score)>();

        foreach (var property in candidates)
        {
            var score = PropertyScorer.Score(property, profile, _mapsService);
            if (score.TotalScore >= MinMatchScore)
                scores.Add((property, score));
        }

        // Sort by score descending
        scores.Sort((a, b) => b.Score.TotalScore.CompareTo(a.Score.TotalScore));

        // Take top N for AI refinement
        var topScores = scores.Take(maxResults * 2).ToList();

        // ── Step 4: Build preliminary RecommendationDtos ───────────────────────
        var preliminaryDtos = new List<(RecommendationDto Dto, PropertyScoreDto Score)>();
        foreach (var (property, score) in topScores)
        {
            var dto = await BuildRecommendationDtoAsync(
                property, score, profile, ct);
            preliminaryDtos.Add((dto, score));
        }

        // ── Step 5: AI refinement via Claude (best-effort) ────────────────────
        var finalDtos = new List<RecommendationDto>();

        if (request?.IncludeExplanations != false &&
            preliminaryDtos.Count >= 3)
        {
            try
            {
                var aiResult = await _claudeClient.RefineRecommendationsAsync(
                    profile, preliminaryDtos, ct);

                if (aiResult is not null)
                {
                    finalDtos = ApplyAiRefinement(
                        preliminaryDtos, aiResult, maxResults);

                    _logger.LogInformation(
                        "AI refinement applied for {StudentId}. " +
                        "Properties={Count}",
                        studentId, finalDtos.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Claude refinement failed for {StudentId} — " +
                    "using rule-based order", studentId);
            }
        }

        // Fallback to rule-based order if AI unavailable
        if (!finalDtos.Any())
        {
            finalDtos = preliminaryDtos
                .Take(maxResults)
                .Select((item, idx) => item.Dto with { Rank = idx + 1 })
                .ToList();
        }

        // ── Step 6: Cache and return ───────────────────────────────────────────
        _cache.Set(cacheKey, (IReadOnlyList<RecommendationDto>)finalDtos,
            TimeSpan.FromMinutes(CacheMinutes));

        _logger.LogInformation(
            "Recommendations generated for {StudentId}. " +
            "Count={Count} ProfileConfidence={Confidence:F1}",
            studentId, finalDtos.Count, profile.ProfileConfidence);

        return ServiceResult<IReadOnlyList<RecommendationDto>>
            .Success(finalDtos);
    }

    // ── GetPreferenceProfileAsync ─────────────────────────────────────────────

    public async Task<ServiceResult<StudentPreferenceProfileDto>>
        GetPreferenceProfileAsync(
            string studentId,
            CancellationToken ct = default)
    {
        var profile = await _analyzer.BuildProfileAsync(studentId, ct);
        return ServiceResult<StudentPreferenceProfileDto>.Success(profile);
    }

    // ── RecordSearchAsync ─────────────────────────────────────────────────────

    public async Task RecordSearchAsync(
        string studentId,
        StudentSearchHistoryEntry entry,
        CancellationToken ct = default)
    {
        try
        {
            var record = new StudentSearchHistory
            {
                StudentId = studentId,
                UniversityId = entry.UniversityId,
                AreaId = entry.AreaId,
                MinPrice = entry.MinPrice,
                MaxPrice = entry.MaxPrice,
                PropertyType = entry.PropertyType,
                SearchedAt = entry.SearchedAt
            };

            await _uow.Repository<StudentSearchHistory>().AddAsync(record, ct);
            await _uow.SaveChangesAsync(ct);

            // Invalidate cached recommendations when student searches
            var cachePattern = $"{CacheKeyPrefix}{studentId}:";
            _cache.Remove(cachePattern);
        }
        catch (Exception ex)
        {
            // Never let search recording block the main search
            _logger.LogWarning(ex,
                "Failed to record search history for {StudentId}", studentId);
        }
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<IReadOnlyList<Property>> LoadCandidatePropertiesAsync(
        string studentId,
        StudentPreferenceProfileDto profile,
        RecommendationRequestDto? request,
        CancellationToken ct)
    {
        // Build a search filter based on the preference profile
        var searchFilter = new PropertySearchFilter
        {
            UniversityId = profile.UniversityId,
            AreaId = request?.AreaId ??
                 (profile.PreferredAreaIds.Any()
                     ? profile.PreferredAreaIds[0]
                     : null),

            MinPrice = profile.InferredMinPrice.HasValue
            ? profile.InferredMinPrice.Value * 0.70m
            : null,

            MaxPrice = request?.MaxPrice ??
                   (profile.InferredMaxPrice.HasValue
                       ? profile.InferredMaxPrice.Value * 1.40m
                       : null)
        };

        var (items, totalCount) = await _uow.Properties.SearchAsync(
       searchFilter,
       1,
       50,
       ct);
        if (items.Any())
            return items;

        // If filtered search returns nothing, load all available properties
        return await _uow.Properties.GetAllApprovedAvailableAsync(ct);
    }

    private async Task<IReadOnlyList<Property>> LoadFallbackPropertiesAsync(
        string studentId,
        StudentPreferenceProfileDto profile,
        CancellationToken ct)
    {
        // Cold start: load top-rated available properties
        // If student has a university, filter by proximity
        var allProperties = await _uow.Properties
            .GetAllApprovedAvailableAsync(ct);

        if (profile.UniversityId.HasValue)
        {
            allProperties = allProperties
                .Where(p => p.UniversityId == profile.UniversityId)
                .ToList();
        }

        return allProperties
            .OrderByDescending(p => p.AverageRating)
           .ThenByDescending(p => p.Reviews.Count)
            .Take(20)
            .ToList();
    }

    private async Task<RecommendationDto> BuildRecommendationDtoAsync(
        Property property,
        PropertyScoreDto score,
        StudentPreferenceProfileDto profile,
        CancellationToken ct)
    {
        // Calculate distance to university for display
        string? distanceText = null;
        if (profile.UniversityLatitude.HasValue &&
            profile.UniversityLongitude.HasValue)
        {
            var distKm = _mapsService.GetStraightLineDistanceKm(
                property.Latitude, property.Longitude,
                profile.UniversityLatitude.Value,
                profile.UniversityLongitude.Value);

            distanceText = distKm < 1.0
                ? $"{(int)(distKm * 1000)}m from university"
                : $"{distKm:F1}km from university";
        }

        // Load area name
        var areaName = property.Area?.NameEn ?? string.Empty;

        return new RecommendationDto
        {
            PropertyId = property.Id,
            Title = property.Title,
            PrimaryImageUrl = property.Images
                                       .FirstOrDefault(i => i.IsPrimary)
                                       ?.ImageUrl,
            AreaNameEn = areaName,
            UniversityNameEn = property.University?.NameEn,
            PropertyTypeDisplay = property.PropertyType.ToString(),
            PricePerMonth = property.PricePerMonth,
            AverageRating = property.AverageRating,
            TotalReviews = property.Reviews.Count,
            IsAvailable = property.IsAvailable,
            Latitude = property.Latitude,
            Longitude = property.Longitude,
            MatchScore = score.TotalScore,
            MatchReasons = score.MatchReasons,
            AiExplanation = null,  // filled by AI step
            Rank = 0,     // filled after sorting
            IsAiRefined = false,
            DistanceToUniversity = distanceText
        };
    }

    private static List<RecommendationDto> ApplyAiRefinement(
        IReadOnlyList<(RecommendationDto Dto, PropertyScoreDto Score)> candidates,
        AiRefinementResult aiResult,
        int maxResults)
    {
        var byId = candidates.ToDictionary(c => c.Dto.PropertyId);
        var result = new List<RecommendationDto>();

        // Apply AI ordering + explanations
        foreach (var propertyId in aiResult.RefinedOrder)
        {
            if (!byId.TryGetValue(propertyId, out var item)) continue;

            var explanation = aiResult.Explanations.TryGetValue(
                propertyId, out var exp) ? exp : null;

            result.Add(item.Dto with
            {
                AiExplanation = explanation,
                IsAiRefined = true,
                Rank = result.Count + 1
            });

            if (result.Count >= maxResults) break;
        }

        // Add any properties not in AI order (AI may have returned partial list)
        foreach (var (dto, _) in candidates)
        {
            if (result.Count >= maxResults) break;
            if (result.Any(r => r.PropertyId == dto.PropertyId)) continue;

            result.Add(dto with { Rank = result.Count + 1 });
        }

        return result;
    }
}