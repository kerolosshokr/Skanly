// Skanly.Application/Features/Chatbot/Knowledge/SkanlyFaqKnowledge.cs
namespace Skanly.Application.Features.Chatbot.Knowledge;

/// <summary>
/// Static FAQ knowledge base embedded into Claude's context.
/// Keeps answers accurate and Skanly-specific.
/// Update this file when platform policies change.
/// </summary>
public static class SkanlyFaqKnowledge
{
    public static readonly IReadOnlyList<FaqEntry> Entries = new List<FaqEntry>
    {
        // ── Verification ──────────────────────────────────────────────────────
        new("verify identity",
            "To verify your identity on Skanly, go to your Profile → " +
            "Verify Identity and upload clear photos of the front (and " +
            "optionally back) of your Egyptian National ID. Our AI will " +
            "scan it automatically, then an Admin will review it within " +
            "24–48 hours. You will receive a notification once approved. " +
            "Identity verification is required before making a booking."),

        new("verification rejected",
            "If your verification was rejected, check the rejection reason " +
            "in your Profile → Verify Identity. Common reasons include: " +
            "blurry image, expired ID, or the ID number not being visible. " +
            "You can resubmit with clearer photos at any time."),

        // ── Booking ───────────────────────────────────────────────────────────
        new("how to book",
            "To book a property on Skanly: " +
            "1. Verify your identity first (required). " +
            "2. Find a property and click 'Request Booking'. " +
            "3. Choose your check-in date and submit. " +
            "4. The owner will accept or reject within 24–48 hours. " +
            "5. If accepted, proceed to payment to confirm your booking. " +
            "Your booking is only confirmed after successful payment."),

        new("booking status",
            "Booking statuses on Skanly: " +
            "• Pending — waiting for the owner to respond. " +
            "• Accepted — owner approved, proceed to payment. " +
            "• Payment Pending — payment started but not completed. " +
            "• Confirmed — payment received, booking is secured. " +
            "• Rejected — owner declined your request. " +
            "• Cancelled — you or the owner cancelled the booking."),

        new("cancel booking",
            "You can cancel a booking while it is Pending or Accepted. " +
            "Once payment is completed (Confirmed status), " +
            "cancellation requires contacting the owner directly. " +
            "Go to My Bookings → select the booking → Cancel."),

        // ── Payment ───────────────────────────────────────────────────────────
        new("payment methods",
            "Skanly accepts: Visa, Mastercard, Vodafone Cash, " +
            "InstaPay, and Fawry. All payments are processed securely. " +
            "You pay the deposit (20% of the monthly rent) to confirm " +
            "your booking."),

        new("how much to pay",
            "When you confirm a booking, you pay a deposit of 20% of the " +
            "monthly rent. For example, if rent is EGP 3,000/month, " +
            "the deposit is EGP 600. The remaining rent is paid directly " +
            "to the owner as per your agreement."),

        new("payment failed",
            "If your payment fails, your booking returns to Accepted status " +
            "and you can try again. Check your card details or try a " +
            "different payment method. If the issue persists, contact " +
            "your bank or try Fawry or Vodafone Cash as an alternative."),

        // ── Contract ──────────────────────────────────────────────────────────
        new("contract",
            "After your payment is confirmed, Skanly automatically generates " +
            "a digital rental contract (PDF). You can download it from " +
            "My Bookings → Booking Details → Download Contract. " +
            "Keep this document safe as proof of your rental agreement."),

        // ── Reviews ───────────────────────────────────────────────────────────
        new("leave review",
            "You can write a review after your booking is Confirmed. " +
            "Go to My Bookings → find the booking → Write Review. " +
            "You can rate Cleanliness, Safety, Internet, Location, " +
            "Quietness, and Overall Experience (1–5 stars each). " +
            "Reviews can be edited once within 30 days of submission."),

        // ── Platform ──────────────────────────────────────────────────────────
        new("commission",
            "Skanly charges owners a platform commission on confirmed " +
            "bookings. Students do not pay any commission — only the " +
            "deposit and monthly rent agreed with the owner."),

        new("report property",
            "To report a suspicious or problematic property, click the " +
            "'Report' button on the property detail page. Select the " +
            "report type (Fake Listing, Property Issue, etc.), describe " +
            "the issue, and optionally attach evidence. Our team reviews " +
            "all reports within 24–48 hours."),

        new("favorite property",
            "To save a property, click the heart ❤️ icon on any property " +
            "card or detail page. Access your saved properties from " +
            "the Favorites menu. Skanly also uses your favorites to " +
            "improve your personalised recommendations."),

        new("chat owner",
            "To chat with a property owner, go to the property detail page " +
            "and click 'Chat with Owner'. You can also access all your " +
            "conversations from the Messages menu. The owner will be " +
            "notified of your message in real time."),

        // ── Account ───────────────────────────────────────────────────────────
        new("change password",
            "To change your password: go to Profile → Security → " +
            "Change Password. You'll need to enter your current password " +
            "and choose a new one. If you forgot your password, use the " +
            "'Forgot Password' link on the login page."),

        new("update profile",
            "Update your profile from Profile → Edit Profile. " +
            "You can change your name, phone number, university, " +
            "and profile photo. Your email address cannot be changed " +
            "after registration — contact support if needed."),
    };

    /// <summary>
    /// Converts all FAQ entries to a compact string for inclusion
    /// in Claude's system prompt.
    /// </summary>
    public static string ToSystemPromptBlock()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Skanly FAQ Knowledge Base");
        sb.AppendLine("Use these answers when relevant. " +
                      "Keep answers concise and in the student's language.\n");

        foreach (var entry in Entries)
        {
            sb.AppendLine($"### {entry.Topic}");
            sb.AppendLine(entry.Answer);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public record FaqEntry(string Topic, string Answer);