// Skanly.Infrastructure/AI/ClaudeRecommendationClient.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skanly.Application.Features.Recommendations.DTOs;
using Skanly.Application.Features.Recommendations.Interfaces;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Skanly.Infrastructure.AI;

/// <summary>
/// Calls the Anthropic Claude API to refine and explain property recommendations.
///
/// The prompt is engineered to:
///   1. Respect the rule-based ranking (don't overturn obvious matches)
///   2. Provide concise, student-friendly explanations (1–2 sentences)
///   3. Return structured JSON — no markdown, no preamble
///   4. Complete within the timeout budget
/// </summary>
public class ClaudeRecommendationClient : IClaudeRecommendationClient
{
    private readonly HttpClient _http;
    private readonly ClaudeClientSettings _settings;
    private readonly ILogger<ClaudeRecommendationClient> _logger;

    private const string AnthropicApiUrl =
        "https://api.anthropic.com/v1/messages";

    public ClaudeRecommendationClient(
        HttpClient http,
        IOptions<ClaudeClientSettings> settings,
        ILogger<ClaudeRecommendationClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AiRefinementResult?> RefineRecommendationsAsync(
        StudentPreferenceProfileDto profile,
        IReadOnlyList<(RecommendationDto Recommendation,
                       PropertyScoreDto Score)> candidates,
        CancellationToken ct = default)
    {
        if (!_settings.EnableAiRefinement)
            return null;

        if (candidates.Count < _settings.MinPropertiesForAi)
            return null;

        try
        {
            var prompt = BuildPrompt(profile, candidates);

            var requestBody = new
            {
                model = _settings.Model,
                max_tokens = _settings.MaxTokens,
                system = BuildSystemPrompt(),
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            var response = await _http.PostAsJsonAsync(
                AnthropicApiUrl, requestBody, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Claude API returned {Status} for recommendations",
                    response.StatusCode);
                return null;
            }

            var responseBody = await response.Content
                .ReadAsStringAsync(cts.Token);

            return ParseClaudeResponse(responseBody);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Claude recommendation API timed out after {Timeout}s",
                _settings.TimeoutSeconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API error in recommendation refinement");
            return null;
        }
    }

    // ── Prompt Engineering ────────────────────────────────────────────────────

    private static string BuildSystemPrompt() => """
        You are a housing recommendation assistant for Skanly,
        a student housing platform in Egypt.

        Your job is to review a list of pre-scored properties and:
        1. Refine the ranking based on the student's profile
        2. Write a short, friendly explanation for each property
           (1–2 sentences, in English, from the student's perspective)

        Rules:
        - Keep explanations concise, specific, and student-friendly
        - Mention concrete details (price, location, amenities) where relevant
        - Do NOT use markdown, bullet points, or headers in explanations
        - Return ONLY valid JSON — no preamble, no commentary
        - If a property doesn't match the student well, you may de-rank it
          but do not exclude it from the output

        Response format (strict JSON, no markdown):
        {
          "refined_order": [propertyId1, propertyId2, ...],
          "explanations": {
            "propertyId1": "explanation text",
            "propertyId2": "explanation text"
          }
        }
        """;

    private static string BuildPrompt(
        StudentPreferenceProfileDto profile,
        IReadOnlyList<(RecommendationDto Recommendation,
                       PropertyScoreDto Score)> candidates)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Student Profile");
        sb.AppendLine($"- Name: {profile.StudentFullName ?? "Student"}");
        sb.AppendLine($"- University: {profile.UniversityNameEn ?? "Not specified"}");

        if (profile.InferredMinPrice.HasValue || profile.InferredMaxPrice.HasValue)
            sb.AppendLine(
                $"- Budget: EGP {profile.InferredMinPrice?.ToString("N0") ?? "any"}" +
                $" – {profile.InferredMaxPrice?.ToString("N0") ?? "any"}/month");

        if (profile.PreferredAreaNames.Any())
            sb.AppendLine(
                $"- Preferred areas: {string.Join(", ", profile.PreferredAreaNames)}");

        if (profile.PreferredPropertyTypes.Any())
            sb.AppendLine(
                $"- Preferred property types: " +
                string.Join(", ", profile.PreferredPropertyTypes));

        sb.AppendLine($"- Behavioral data: " +
                      $"{profile.TotalFavorites} favorites, " +
                      $"{profile.TotalBookings} bookings, " +
                      $"{profile.TotalSearches} searches");

        sb.AppendLine("\n## Candidate Properties (already sorted by match score)");

        foreach (var (rec, score) in candidates)
        {
            sb.AppendLine($"\n### Property ID: {rec.PropertyId}");
            sb.AppendLine($"- Title: {rec.Title}");
            sb.AppendLine($"- Area: {rec.AreaNameEn}");
            sb.AppendLine($"- Type: {rec.PropertyTypeDisplay}");
            sb.AppendLine($"- Price: EGP {rec.PricePerMonth:N0}/month");
            sb.AppendLine($"- Rating: {rec.AverageRating:F1}/5 ({rec.TotalReviews} reviews)");

            if (rec.DistanceToUniversity is not null)
                sb.AppendLine($"- Distance: {rec.DistanceToUniversity}");

            sb.AppendLine($"- Match score: {score.TotalScore:F0}/100");

            if (rec.MatchReasons.Any())
                sb.AppendLine(
                    $"- Rule-based reasons: {string.Join("; ", rec.MatchReasons)}");
        }

        sb.AppendLine(
            "\nPlease refine the ranking and write an explanation for each. " +
            "Return only the JSON object as specified.");

        return sb.ToString();
    }

    // ── Response Parsing ──────────────────────────────────────────────────────

    private AiRefinementResult? ParseClaudeResponse(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            // Strip any accidental markdown fences
            var json = content
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;

            // Parse refined order
            var refinedOrder = root
                .GetProperty("refined_order")
                .EnumerateArray()
                .Select(el => el.GetInt32())
                .ToList();

            // Parse explanations
            var explanations = new Dictionary<int, string>();
            foreach (var prop in root
                .GetProperty("explanations")
                .EnumerateObject())
            {
                if (int.TryParse(prop.Name, out var propId))
                    explanations[propId] = prop.Value.GetString() ?? "";
            }

            return new AiRefinementResult
            {
                RefinedOrder = refinedOrder,
                Explanations = explanations
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to parse Claude recommendation response");
            return null;
        }
    }
}