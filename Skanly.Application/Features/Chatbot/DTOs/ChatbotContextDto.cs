// Skanly.Application/Features/Chatbot/DTOs/ChatbotContextDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Chatbot.DTOs;

/// <summary>
/// All context injected into the system prompt for every conversation turn.
/// Tells Claude who the student is, what they care about, and what page
/// they are on — so Claude can give specific, grounded answers.
/// </summary>
public class ChatbotContextDto
{
    // ── Student identity ──────────────────────────────────────────────────────
    public string StudentId { get; init; } = string.Empty;
    public string StudentFullName { get; init; } = string.Empty;
    public bool IsIdentityVerified { get; init; }
    public string? UniversityNameEn { get; init; }
    public string? PreferredAreaNameEn { get; init; }
    public decimal? InferredBudget { get; init; }

    // ── Current page context ──────────────────────────────────────────────────
    public PropertyContextDto? CurrentProperty { get; init; }

    // ── Booking context ───────────────────────────────────────────────────────
    public IReadOnlyList<ActiveBookingContextDto> ActiveBookings { get; init; }
        = new List<ActiveBookingContextDto>();

    // ── Recommendation snapshot ───────────────────────────────────────────────
    public IReadOnlyList<string> TopRecommendedPropertyTitles { get; init; }
        = new List<string>();
}

public class PropertyContextDto
{
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal PricePerMonth { get; init; }
    public string AreaNameEn { get; init; } = string.Empty;
    public string PropertyTypeDisplay { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public string GenderPolicyDisplay { get; init; } = string.Empty;
    public IReadOnlyList<string> AmenityNames { get; init; }
        = new List<string>();
    public bool IsAvailable { get; init; }
    public string OwnerFullName { get; init; } = string.Empty;
    public string? DistanceToUniversity { get; init; }
}

public class ActiveBookingContextDto
{
    public int BookingId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public string StatusDisplay { get; init; } = string.Empty;
    public DateOnly CheckInDate { get; init; }
    public decimal TotalAmount { get; init; }
    public bool CanPay { get; init; }
}