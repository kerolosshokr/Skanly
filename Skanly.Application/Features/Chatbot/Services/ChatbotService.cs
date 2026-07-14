// Skanly.Application/Features/Chatbot/Services/ChatbotService.cs
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Bookings.Interfaces;
using Skanly.Application.Features.Chatbot.DTOs;
using Skanly.Application.Features.Chatbot.Interfaces;
using Skanly.Application.Features.Chatbot.Knowledge;
using Skanly.Application.Features.Maps.Interfaces;
using Skanly.Application.Features.Recommendations.Interfaces;
using Skanly.Application.Features.Recommendations.Services;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using System.Text;

namespace Skanly.Application.Features.Chatbot.Services;

public class ChatbotService : IChatbotService
{
    private readonly IUnitOfWork _uow;
    private readonly IClaudeChatClient _claudeClient;
    private readonly ChatbotIntentRouter _intentRouter;
    private readonly StudentPreferenceAnalyzer _preferenceAnalyzer;
    private readonly IGoogleMapsService _mapsService;
    private readonly ILogger<ChatbotService> _logger;

    // Max messages kept in Claude's context window per conversation
    private const int MaxHistoryMessages = 20;
    private const int MaxMessageLength = 1000;

    public ChatbotService(
        IUnitOfWork uow,
        IClaudeChatClient claudeClient,
        ChatbotIntentRouter intentRouter,
        StudentPreferenceAnalyzer preferenceAnalyzer,
        IGoogleMapsService mapsService,
        ILogger<ChatbotService> logger)
    {
        _uow = uow;
        _claudeClient = claudeClient;
        _intentRouter = intentRouter;
        _preferenceAnalyzer = preferenceAnalyzer;
        _mapsService = mapsService;
        _logger = logger;
    }

    // ── SendMessageAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<ChatbotMessageDto>> SendMessageAsync(
        string userId,
        SendChatbotMessageDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate message
        if (string.IsNullOrWhiteSpace(dto.Message))
            return ServiceResult<ChatbotMessageDto>.Failure(
                "Message cannot be empty.");

        if (dto.Message.Length > MaxMessageLength)
            return ServiceResult<ChatbotMessageDto>.Failure(
                $"Message is too long. Maximum {MaxMessageLength} characters.");

        // 2. Get or create conversation
        var conversation = await GetOrCreateConversationAsync(
            userId, dto.ConversationId, dto.CurrentPropertyId, ct);

        // 3. Build context
        var context = await BuildContextAsync(
            userId, dto.CurrentPropertyId ?? conversation.RelatedPropertyId, ct);

        // 4. Persist user message
        var userMessage = new ChatbotMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = dto.Message.Trim(),
            SentAt = DateTime.UtcNow
        };
        await _uow.Repository<ChatbotMessage>().AddAsync(userMessage, ct);
        await _uow.SaveChangesAsync(ct);

        // 5. Try instant answer first (no API call)
        var instantAnswer = await _intentRouter.TryRouteAsync(
            userId, dto.Message, context, ct);

        if (instantAnswer is not null)
        {
            var assistantMsg = await PersistAssistantMessageAsync(
                conversation.Id,
                instantAnswer.Content,
                instantAnswer.Intent,
                isInstant: true,
                tokensUsed: null,
                ct);

            UpdateConversationTimestamp(conversation);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<ChatbotMessageDto>.Success(
                MapToDto(assistantMsg));
        }

        // 6. Build conversation history for Claude
        var history = await BuildHistoryAsync(
            conversation.Id, ct);

        // 7. Build system prompt
        var systemPrompt = BuildSystemPrompt(context);

        // 8. Call Claude
        ClaudeResponse? response = null;
        try
        {
            response = await _claudeClient.SendAsync(
                systemPrompt, history, dto.Message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Claude API error in chatbot for user {UserId}", userId);

            // Return a graceful fallback
            var fallback = await PersistAssistantMessageAsync(
                conversation.Id,
                "I'm having trouble connecting right now. " +
                "Please try again in a moment, or browse our " +
                "[FAQ](/faq) for quick answers.",
                null, false, null, ct);

            UpdateConversationTimestamp(conversation);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<ChatbotMessageDto>.Success(MapToDto(fallback));
        }

        // 9. Persist assistant response
        var assistantMessage = await PersistAssistantMessageAsync(
            conversation.Id,
            response?.Content ?? "I'm not sure how to help with that. " +
                                 "Try asking in a different way.",
            null, false, response?.TokensUsed, ct);

        UpdateConversationTimestamp(conversation);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Chatbot response sent for user {UserId}. " +
            "Tokens={Tokens} ConvId={ConvId}",
            userId, response?.TokensUsed, conversation.Id);

        return ServiceResult<ChatbotMessageDto>.Success(
            MapToDto(assistantMessage));
    }

    // ── StreamMessageAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<int>> StreamMessageAsync(
        string userId,
        SendChatbotMessageDto dto,
        Func<string, Task> onToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return ServiceResult<int>.Failure("Message cannot be empty.");

        // 1. Get or create conversation
        var conversation = await GetOrCreateConversationAsync(
            userId, dto.ConversationId, dto.CurrentPropertyId, ct);

        // 2. Build context
        var context = await BuildContextAsync(
            userId, dto.CurrentPropertyId ?? conversation.RelatedPropertyId, ct);

        // 3. Persist user message
        var userMessage = new ChatbotMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = dto.Message.Trim(),
            SentAt = DateTime.UtcNow
        };
        await _uow.Repository<ChatbotMessage>().AddAsync(userMessage, ct);
        await _uow.SaveChangesAsync(ct);

        // 4. Try instant answer
        var instantAnswer = await _intentRouter.TryRouteAsync(
            userId, dto.Message, context, ct);

        if (instantAnswer is not null)
        {
            // Stream the instant answer character by character
            // (gives consistent UX even for non-Claude responses)
            foreach (var chunk in SplitIntoChunks(instantAnswer.Content, 20))
            {
                await onToken(chunk);
                await Task.Delay(15, ct);  // slight delay for natural feel
            }

            await PersistAssistantMessageAsync(
                conversation.Id, instantAnswer.Content,
                instantAnswer.Intent, true, null, ct);

            UpdateConversationTimestamp(conversation);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<int>.Success(conversation.Id);
        }

        // 5. Build history and system prompt
        var history = await BuildHistoryAsync(conversation.Id, ct);
        var systemPrompt = BuildSystemPrompt(context);

        // 6. Stream from Claude
        var fullResponseBuilder = new StringBuilder();
        int? tokensUsed = null;

        try
        {
            await _claudeClient.StreamAsync(
                systemPrompt,
                history,
                dto.Message,
                onToken: async token =>
                {
                    fullResponseBuilder.Append(token);
                    await onToken(token);
                },
                onComplete: async tokens =>
                {
                    tokensUsed = tokens;
                    await Task.CompletedTask;
                },
                ct: ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — still persist what we have
            _logger.LogDebug(
                "Streaming cancelled for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Streaming error for user {UserId}", userId);

            var errorMsg = "I ran into an issue. Please try again.";
            await onToken(errorMsg);
            fullResponseBuilder.Append(errorMsg);
        }

        // 7. Persist full response
        var fullResponse = fullResponseBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(fullResponse))
        {
            await PersistAssistantMessageAsync(
                conversation.Id, fullResponse, null,
                false, tokensUsed, ct);

            UpdateConversationTimestamp(conversation);
            await _uow.SaveChangesAsync(ct);
        }

        return ServiceResult<int>.Success(conversation.Id);
    }

    // ── GetConversationAsync ──────────────────────────────────────────────────

    public async Task<ServiceResult<ChatbotConversationDto>>
        GetConversationAsync(
            string userId,
            int conversationId,
            CancellationToken ct = default)
    {
        var conversation = await _uow.Repository<ChatbotConversation>()
            .GetFirstOrDefaultAsync(
                c => c.Id == conversationId && c.UserId == userId,
                ct,
                c => c.Messages,
                c => c.RelatedProperty!);

        if (conversation is null)
            return ServiceResult<ChatbotConversationDto>.Failure(
                "Conversation not found.");

        return ServiceResult<ChatbotConversationDto>.Success(
            MapConversationToDto(conversation));
    }

    // ── GetRecentConversationsAsync ───────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<ChatbotConversationDto>>>
        GetRecentConversationsAsync(
            string userId,
            int count = 5,
            CancellationToken ct = default)
    {
        var conversations = await _uow.Repository<ChatbotConversation>()
     .GetAllAsync(
         c => c.UserId == userId && c.IsActive,
         null,
         ct,
         c => c.Messages);

        var ordered = conversations
            .OrderByDescending(c => c.LastMessageAt)
            .Take(count)
            .Select(MapConversationToDto)
            .ToList();

        return ServiceResult<IReadOnlyList<ChatbotConversationDto>>
            .Success(ordered);
    }

    // ── StartNewConversationAsync ─────────────────────────────────────────────

    public async Task<ServiceResult<int>> StartNewConversationAsync(
        string userId,
        int? relatedPropertyId = null,
        CancellationToken ct = default)
    {
        var conversation = new ChatbotConversation
        {
            UserId = userId,
            RelatedPropertyId = relatedPropertyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };

        await _uow.Repository<ChatbotConversation>()
            .AddAsync(conversation, ct);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult<int>.Success(conversation.Id);
    }

    // ── BuildContextAsync ─────────────────────────────────────────────────────

    public async Task<ChatbotContextDto> BuildContextAsync(
        string userId,
        int? currentPropertyId = null,
        CancellationToken ct = default)
    {
        // Load student
        var student = await _uow.Students
            .GetWithUniversityAsync(userId, ct);

        // Load active bookings (Pending, Accepted, PaymentPending)
        var activeStatuses = new[]
        {
            BookingStatus.Pending,
            BookingStatus.Accepted,
            BookingStatus.PaymentPending
        };

        var activeBookings = await _uow.Repository<Booking>()
      .GetAllAsync(
          b => b.StudentId == userId &&
               activeStatuses.Contains(b.Status),
          null,
          ct,
          b => b.Property);

        var bookingContexts = activeBookings.Select(b =>
            new ActiveBookingContextDto
            {
                BookingId = b.Id,
                PropertyTitle = b.Property?.Title ?? string.Empty,
                StatusDisplay = b.Status.ToString(),
                CheckInDate = b.CheckInDate,
                TotalAmount = b.TotalAmount,
                CanPay = b.Status == BookingStatus.Accepted ||
                               b.Status == BookingStatus.PaymentPending
            }).ToList();

        // Load current property context
        PropertyContextDto? propertyContext = null;
        if (currentPropertyId.HasValue)
        {
            var property = await _uow.Properties
                .GetDetailAsync(currentPropertyId.Value, ct);

            if (property is not null)
            {
                string? distanceText = null;
                if (student?.University is not null)
                {
                    var dist = _mapsService.GetStraightLineDistanceKm(
                        property.Latitude, property.Longitude,
                        student.University.Latitude,
                        student.University.Longitude);
                    distanceText = dist < 1.0
                        ? $"{(int)(dist * 1000)}m"
                        : $"{dist:F1}km";
                }

                propertyContext = new PropertyContextDto
                {
                    PropertyId = property.Id,
                    Title = property.Title,
                    PricePerMonth = property.PricePerMonth,
                    AreaNameEn = property.Area?.NameEn ?? "",
                    PropertyTypeDisplay = property.PropertyType.ToString(),
                    AverageRating = property.AverageRating,
                    TotalReviews = property.Reviews.Count,

                    AmenityNames = property.PropertyAmenities
                    .Select(pa => pa.Amenity.NameEn)
                       .ToList(),
                    IsAvailable = property.IsAvailable,
                    OwnerFullName = property.Owner?.FullName ?? "",
                    DistanceToUniversity = distanceText
                };
            }
        }

        // Build preference profile for budget inference
        var profile = await _preferenceAnalyzer
            .BuildProfileAsync(userId, ct);

        return new ChatbotContextDto
        {
            StudentId = userId,
            StudentFullName = student?.FullName ?? "Student",
            IsIdentityVerified = student?.IsIdentityVerified ?? false,
            UniversityNameEn = student?.University?.NameEn,
            InferredBudget = profile.InferredMaxPrice,
            CurrentProperty = propertyContext,
            ActiveBookings = bookingContexts
        };
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SYSTEM PROMPT ENGINEERING
    // ══════════════════════════════════════════════════════════════════════════

    private static string BuildSystemPrompt(ChatbotContextDto context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("""
            You are Sana, the friendly AI assistant for Skanly — Egypt's
            smart student housing platform.

            Your personality:
            - Warm, helpful, and concise
            - Knowledgeable about the Skanly platform and student life in Egypt
            - Direct: give specific answers, not vague generalities
            - Use simple, clear language (the student may prefer Arabic or English)
            - If the student writes in Arabic, respond in Arabic
            - Never pretend to be human — you are an AI assistant

            Your capabilities:
            - Answer questions about Skanly features (booking, payment, verification)
            - Help students find suitable housing based on their needs
            - Explain booking statuses and guide students through the process
            - Answer questions about a specific property the student is viewing
            - Provide general advice about student life and renting in Egypt

            Your limitations:
            - You cannot make bookings or payments on behalf of the student
            - You cannot access information not provided in this context
            - For urgent issues or disputes, direct students to contact support
            - Do not provide legal advice or specific financial recommendations

            Formatting rules:
            - Use **bold** for property names, amounts, and important actions
            - Use bullet points sparingly — prefer natural prose
            - Include clickable links in Markdown format [text](url) where relevant
            - Keep responses under 200 words unless the question genuinely requires more
            - End with a follow-up question or offer to help further when appropriate
            """);

        // ── Student context ────────────────────────────────────────────────────
        sb.AppendLine("\n## Current Student");
        sb.AppendLine($"- Name: {context.StudentFullName}");
        sb.AppendLine($"- Identity Verified: {(context.IsIdentityVerified ? "Yes ✅" : "No ❌")}");

        if (context.UniversityNameEn is not null)
            sb.AppendLine($"- University: {context.UniversityNameEn}");

        if (context.InferredBudget.HasValue)
            sb.AppendLine(
                $"- Estimated budget: up to EGP {context.InferredBudget:N0}/month");

        // ── Active bookings ────────────────────────────────────────────────────
        if (context.ActiveBookings.Any())
        {
            sb.AppendLine("\n## Student's Active Bookings");
            foreach (var b in context.ActiveBookings)
            {
                sb.AppendLine(
                    $"- Booking #{b.BookingId}: {b.PropertyTitle} " +
                    $"({b.StatusDisplay}) — " +
                    $"check-in {b.CheckInDate:MMM dd}, " +
                    $"EGP {b.TotalAmount:N0}" +
                    $"{(b.CanPay ? " [PAYMENT DUE]" : "")}");
            }
        }

        // ── Current property context ───────────────────────────────────────────
        if (context.CurrentProperty is not null)
        {
            var p = context.CurrentProperty;
            sb.AppendLine("\n## Property Currently Being Viewed");
            sb.AppendLine($"- Title: {p.Title}");
            sb.AppendLine($"- Price: EGP {p.PricePerMonth:N0}/month");
            sb.AppendLine($"- Area: {p.AreaNameEn}");
            sb.AppendLine($"- Type: {p.PropertyTypeDisplay}");
            sb.AppendLine($"- Rating: {p.AverageRating:F1}/5 " +
                          $"({p.TotalReviews} reviews)");
            sb.AppendLine($"- Gender Policy: {p.GenderPolicyDisplay}");
            sb.AppendLine($"- Available: {(p.IsAvailable ? "Yes" : "No")}");
            sb.AppendLine($"- Owner: {p.OwnerFullName}");

            if (p.DistanceToUniversity is not null)
                sb.AppendLine($"- Distance to university: {p.DistanceToUniversity}");

            if (p.AmenityNames.Any())
                sb.AppendLine(
                    $"- Amenities: {string.Join(", ", p.AmenityNames)}");
        }

        // ── FAQ knowledge base ─────────────────────────────────────────────────
        sb.AppendLine();
        sb.Append(SkanlyFaqKnowledge.ToSystemPromptBlock());

        return sb.ToString();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<ChatbotConversation> GetOrCreateConversationAsync(
        string userId,
        int? conversationId,
        int? relatedPropertyId,
        CancellationToken ct)
    {
        if (conversationId.HasValue)
        {
            var existing = await _uow.Repository<ChatbotConversation>()
                .GetFirstOrDefaultAsync(
                    c => c.Id == conversationId && c.UserId == userId,
                    ct);

            if (existing is not null) return existing;
        }

        // Create new conversation
        var conv = new ChatbotConversation
        {
            UserId = userId,
            RelatedPropertyId = relatedPropertyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };

        await _uow.Repository<ChatbotConversation>().AddAsync(conv, ct);
        await _uow.SaveChangesAsync(ct);
        return conv;
    }

    private async Task<IReadOnlyList<ClaudeMessage>> BuildHistoryAsync(
        int conversationId,
        CancellationToken ct)
    {
        var messages = await _uow.Repository<ChatbotMessage>()
            .GetAllAsync(m => m.ConversationId == conversationId, ct);

        return messages
            .OrderBy(m => m.SentAt)
            .TakeLast(MaxHistoryMessages)
            .Select(m => new ClaudeMessage(m.Role, m.Content))
            .ToList();
    }

    private async Task<ChatbotMessage> PersistAssistantMessageAsync(
        int conversationId,
        string content,
        string? intent,
        bool isInstant,
        int? tokensUsed,
        CancellationToken ct)
    {
        var message = new ChatbotMessage
        {
            ConversationId = conversationId,
            Role = "assistant",
            Content = content,
            DetectedIntent = intent,
            IsInstantAnswer = isInstant,
            TokensUsed = tokensUsed,
            SentAt = DateTime.UtcNow
        };

        await _uow.Repository<ChatbotMessage>().AddAsync(message, ct);
        return message;
    }

    private static void UpdateConversationTimestamp(
        ChatbotConversation conversation)
    {
        conversation.LastMessageAt = DateTime.UtcNow;
    }

    private static ChatbotMessageDto MapToDto(ChatbotMessage m) => new()
    {
        MessageId = m.Id,
        ConversationId = m.ConversationId,
        Role = m.Role,
        Content = m.Content,
        IsInstantAnswer = m.IsInstantAnswer,
        DetectedIntent = m.DetectedIntent,
        SentAt = m.SentAt
    };

    private static ChatbotConversationDto MapConversationToDto(
        ChatbotConversation c)
    {
        var lastMsg = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

        return new ChatbotConversationDto
        {
            ConversationId = c.Id,
            ConversationTitle = c.ConversationTitle,
            RelatedPropertyId = c.RelatedPropertyId,
            RelatedPropertyTitle = c.RelatedProperty?.Title,
            Messages = c.Messages
                                    .OrderBy(m => m.SentAt)
                                    .Select(MapToDto)
                                    .ToList(),
            LastMessageAt = c.LastMessageAt,
            LastMessagePreview = lastMsg?.Content.Length > 60
                ? lastMsg.Content[..60] + "…"
                : lastMsg?.Content ?? ""
        };
    }

    private static IEnumerable<string> SplitIntoChunks(
        string text, int chunkSize)
    {
        for (int i = 0; i < text.Length; i += chunkSize)
            yield return text[i..Math.Min(i + chunkSize, text.Length)];
    }
}