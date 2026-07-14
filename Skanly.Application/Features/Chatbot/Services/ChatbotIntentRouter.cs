// Skanly.Application/Features/Chatbot/Services/ChatbotIntentRouter.cs
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Features.Chatbot.DTOs;

namespace Skanly.Application.Features.Chatbot.Services;

/// <summary>
/// Lightweight keyword-based intent classifier.
/// Routes simple, deterministic queries to instant DB answers
/// so we avoid unnecessary Claude API calls for common questions.
///
/// Any intent not matched here falls through to Claude.
/// </summary>
public class ChatbotIntentRouter
{
    private readonly IUnitOfWork _uow;

    // Intent → keywords
    private static readonly Dictionary<string, string[]> IntentKeywords = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["booking.status"] = new[] { "booking status", "my booking", "bookings" },
        ["booking.pending"] = new[] { "pending booking", "waiting owner", "no response" },
        ["booking.pay"] = new[] { "pay now", "how to pay", "payment link",
                                        "proceed to payment", "pay deposit" },
        ["verification.check"] = new[] { "is my id verified", "verification status",
                                        "am i verified", "my verification" },
        ["favorites.list"] = new[] { "my favorites", "saved properties",
                                        "my saved", "favourites" },
        ["property.available"] = new[] { "is this available", "still available",
                                        "can i book this", "book this property" },
    };

    public ChatbotIntentRouter(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>
    /// Tries to answer the message instantly without calling Claude.
    /// Returns null if the intent is unknown or requires Claude.
    /// </summary>
    public async Task<InstantAnswer?> TryRouteAsync(
        string userId,
        string message,
        ChatbotContextDto context,
        CancellationToken ct = default)
    {
        var intent = DetectIntent(message);
        if (intent is null) return null;

        return intent switch
        {
            "booking.status" => await AnswerBookingStatusAsync(
                                        userId, context, ct),
            "booking.pending" => await AnswerPendingBookingsAsync(
                                        userId, context, ct),
            "booking.pay" => AnswerBookingPay(context),
            "verification.check" => AnswerVerificationCheck(context),
            "favorites.list" => await AnswerFavoritesAsync(userId, ct),
            "property.available" => AnswerPropertyAvailability(context),
            _ => null
        };
    }

    // ── Intent Detection ──────────────────────────────────────────────────────

    private static string? DetectIntent(string message)
    {
        var lower = message.ToLowerInvariant();

        foreach (var (intent, keywords) in IntentKeywords)
        {
            if (keywords.Any(kw => lower.Contains(kw)))
                return intent;
        }

        return null;
    }

    // ── Instant Answer Builders ───────────────────────────────────────────────

    private async Task<InstantAnswer?> AnswerBookingStatusAsync(
        string userId,
        ChatbotContextDto context,
        CancellationToken ct)
    {
        if (!context.ActiveBookings.Any())
            return new InstantAnswer(
                "booking.status",
                "You don't have any active bookings at the moment. " +
                "Would you like help finding a property to book?");

        var lines = context.ActiveBookings.Select(b =>
            $"• **{b.PropertyTitle}** — {b.StatusDisplay}" +
            $" (Check-in: {b.CheckInDate:MMM dd, yyyy}, " +
            $"Total: EGP {b.TotalAmount:N0})");

        var summary = "Here are your current bookings:\n" +
                      string.Join("\n", lines);

        // If any booking can be paid right now, add a CTA
        var payable = context.ActiveBookings.FirstOrDefault(b => b.CanPay);
        if (payable is not null)
            summary += $"\n\nYour booking for **{payable.PropertyTitle}** " +
                       "is accepted and waiting for payment. " +
                       $"[Pay Now](/Student/Bookings/Details/{payable.BookingId})";

        return new InstantAnswer("booking.status", summary);
    }

    private async Task<InstantAnswer?> AnswerPendingBookingsAsync(
        string userId,
        ChatbotContextDto context,
        CancellationToken ct)
    {
        var pending = context.ActiveBookings
            .Where(b => b.StatusDisplay == "Pending")
            .ToList();

        if (!pending.Any())
            return new InstantAnswer(
                "booking.pending",
                "You have no pending booking requests right now.");

        var lines = pending.Select(b =>
            $"• **{b.PropertyTitle}** — waiting for owner response");

        return new InstantAnswer(
            "booking.pending",
            "Your pending booking requests:\n" +
            string.Join("\n", lines) +
            "\n\nOwners typically respond within 24–48 hours. " +
            "You can cancel a pending request from My Bookings if needed.");
    }

    private static InstantAnswer? AnswerBookingPay(ChatbotContextDto context)
    {
        var payable = context.ActiveBookings.FirstOrDefault(b => b.CanPay);

        if (payable is null)
            return null; // Let Claude handle — may need full context

        return new InstantAnswer(
            "booking.pay",
            $"Your booking for **{payable.PropertyTitle}** is accepted " +
            "and ready for payment. " +
            $"[Click here to pay EGP {payable.TotalAmount * 0.20m:N0} deposit]" +
            $"(/Student/Bookings/Details/{payable.BookingId})");
    }

    private static InstantAnswer AnswerVerificationCheck(
        ChatbotContextDto context)
    {
        if (context.IsIdentityVerified)
            return new InstantAnswer(
                "verification.check",
                "✅ Your identity is verified! You have full access to " +
                "all Skanly features including bookings.");

        return new InstantAnswer(
            "verification.check",
            "Your identity is not yet verified. To unlock bookings, " +
            "go to [Profile → Verify Identity](/Student/Verification) " +
            "and upload your National ID. Verification takes 24–48 hours.");
    }

    private async Task<InstantAnswer> AnswerFavoritesAsync(
        string userId,
        CancellationToken ct)
    {
        var count = await _uow.Favorites.CountAsync(
            f => f.StudentId == userId, ct);

        if (count == 0)
            return new InstantAnswer(
                "favorites.list",
                "You haven't saved any properties yet. " +
                "Click the ❤️ heart icon on any property to save it. " +
                "Saved properties appear in your Favorites page and " +
                "help personalise your recommendations.");

        return new InstantAnswer(
            "favorites.list",
            $"You have **{count}** saved propert{(count == 1 ? "y" : "ies")}. " +
            "[View your Favorites](/Student/Favorites)");
    }

    private static InstantAnswer? AnswerPropertyAvailability(
        ChatbotContextDto context)
    {
        if (context.CurrentProperty is null) return null;

        var prop = context.CurrentProperty;

        if (!prop.IsAvailable)
            return new InstantAnswer(
                "property.available",
                $"**{prop.Title}** is currently unavailable — " +
                "it may already be booked. " +
                "Would you like me to suggest similar properties?");

        return new InstantAnswer(
            "property.available",
            $"Yes, **{prop.Title}** is available! " +
            $"It costs EGP {prop.PricePerMonth:N0}/month. " +
            $"[Request a Booking](/Student/Bookings/Create?propertyId={prop.PropertyId})");
    }
}

public record InstantAnswer(string Intent, string Content);