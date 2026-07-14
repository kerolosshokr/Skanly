// Skanly.Application/Features/Chat/Services/ChatService.cs
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Chat.DTOs;
using Skanly.Application.Features.Chat.Interfaces;
using Skanly.Application.Features.Notifications.Interfaces;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Chat.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<SendMessageDto> _validator;
    private readonly ILogger<ChatService> _logger;
    private readonly INotificationService _notificationService;

    public ChatService(
        IUnitOfWork uow,
        IFileStorageService fileStorage,
        IValidator<SendMessageDto> validator,
        ILogger<ChatService> logger,
        INotificationService notificationService)
    {
        _uow = uow;
        _fileStorage = fileStorage;
        _validator = validator;
        _logger = logger;
        _notificationService = notificationService;
    }

    // ── GetOrCreateConversationAsync ──────────────────────────────────────────

    public async Task<ServiceResult<ConversationDto>> GetOrCreateConversationAsync(
        string requesterId,
        StartConversationDto dto,
        CancellationToken ct = default)
    {
        // Requester must be a Student (owners don't initiate)
        var student = await _uow.Students.GetByUserIdAsync(requesterId, ct);
        if (student is null)
            return ServiceResult<ConversationDto>.Failure(
                "Only students can start conversations.");

        var owner = await _uow.Owners.GetByUserIdAsync(dto.OwnerId, ct);
        if (owner is null)
            return ServiceResult<ConversationDto>.Failure("Owner not found.");

        // Property guard (optional — if supplied, must be owned by the owner)
        if (dto.PropertyId.HasValue)
        {
            var property = await _uow.Properties.GetByIdAsync(dto.PropertyId.Value, ct);
            if (property is null || property.OwnerId != dto.OwnerId)
                return ServiceResult<ConversationDto>.Failure(
                    "Property not found or does not belong to this owner.");
        }

        var conversation = await _uow.Chat.GetOrCreateConversationAsync(
            requesterId, dto.OwnerId, dto.PropertyId, ct);

        var convDto = await BuildConversationDtoAsync(
            conversation, requesterId, ct);

        return ServiceResult<ConversationDto>.Success(convDto);
    }

    // ── GetConversationsAsync ─────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<ConversationDto>>> GetConversationsAsync(
        string userId,
        CancellationToken ct = default)
    {
        var conversations = await _uow.Chat
            .GetConversationsForUserAsync(userId, ct);

        var dtos = new List<ConversationDto>();
        foreach (var conv in conversations)
        {
            var dto = await BuildConversationDtoAsync(conv, userId, ct);
            dtos.Add(dto);
        }

        return ServiceResult<IReadOnlyList<ConversationDto>>.Success(dtos);
    }

    // ── GetConversationByIdAsync ──────────────────────────────────────────────

    public async Task<ServiceResult<ConversationDto>> GetConversationByIdAsync(
        string userId,
        int conversationId,
        CancellationToken ct = default)
    {
        var conv = await _uow.Repository<ChatConversation>()
            .GetFirstOrDefaultAsync(
                c => c.Id == conversationId &&
                     (c.StudentId == userId || c.OwnerId == userId),
                ct,
                c => c.Student,
                c => c.Owner,
                c => c.Property!,
                c => c.Property!.Images,
                c => c.Messages);

        if (conv is null)
            return ServiceResult<ConversationDto>.Failure(
                "Conversation not found or access denied.");

        var dto = await BuildConversationDtoAsync(conv, userId, ct);
        return ServiceResult<ConversationDto>.Success(dto);
    }

    // ── GetMessagesAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<MessageDto>>> GetMessagesAsync(
        string userId,
        int conversationId,
        int pageNumber = 1,
        int pageSize = 40,
        CancellationToken ct = default)
    {
        // Guard: user must be part of this conversation
        var isMember = await _uow.Repository<ChatConversation>()
            .ExistsAsync(c => c.Id == conversationId &&
                              (c.StudentId == userId || c.OwnerId == userId), ct);

        if (!isMember)
            return ServiceResult<PagedResult<MessageDto>>.Failure(
                "Access denied.");

        var (messages, total) = await _uow.Chat
            .GetMessagesAsync(conversationId, pageNumber, pageSize, ct);

        // Build sender display info for each message
        var studentCache = new Dictionary<string, (string Name, string? Image)>();
        var ownerCache = new Dictionary<string, (string Name, string? Image)>();

        var dtos = new List<MessageDto>();
        foreach (var m in messages)
        {
            var (name, image) = await ResolveSenderAsync(
                m.SenderId, studentCache, ownerCache, ct);

            dtos.Add(new MessageDto
            {
                MessageId = m.MessageId,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderFullName = name,
                SenderImageUrl = image,
                MessageText = m.MessageText,
                ImageUrl = m.ImageUrl,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            });
        }

        return ServiceResult<PagedResult<MessageDto>>.Success(
            PagedResult<MessageDto>.Create(dtos, total, pageNumber, pageSize));
    }

    // ── SendMessageAsync ──────────────────────────────────────────────────────

    public async Task<ServiceResult<MessageDto>> SendMessageAsync(
        string senderId,
        SendMessageDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate
        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<MessageDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Verify sender is member of conversation
        var conversation = await _uow.Repository<ChatConversation>()
            .GetFirstOrDefaultAsync(
                c => c.Id == dto.ConversationId &&
                     (c.StudentId == senderId || c.OwnerId == senderId),
                ct);

        if (conversation is null)
            return ServiceResult<MessageDto>.Failure(
                "Conversation not found or access denied.");

        // 3. Upload image if present
        string? imageUrl = null;
        if (dto.Image is not null)
        {
            imageUrl = await _fileStorage.SaveAsync(
                dto.Image,
                $"chat/{dto.ConversationId}",
                ct);
        }

        // 4. Persist message
        var message = new ChatMessage
        {
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            MessageText = dto.MessageText?.Trim(),
            ImageUrl = imageUrl,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        await _uow.Repository<ChatMessage>().AddAsync(message, ct);

        // 5. Update conversation LastMessageAt
        conversation.LastMessageAt = message.SentAt;
        _uow.Repository<ChatConversation>().Update(conversation);

        await _uow.SaveChangesAsync(ct);

        // 6. Build recipient ID for notification (other party in conversation)
        var recipientId = senderId == conversation.StudentId
            ? conversation.OwnerId
            : conversation.StudentId;

        // 7. Persist notification (for offline delivery — SignalR handles online)
        var (senderName, _) = await ResolveSenderDirectAsync(senderId, ct);
        await _notificationService.SendNewMessageAsync(
        recipientId,
        dto.ConversationId,
        senderName,
        dto.Image != null
            ? "📷 Sent you a photo."
            : dto.MessageText ?? "",
        ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Message {MsgId} sent in conversation {ConvId} by {SenderId}",
            message.MessageId, dto.ConversationId, senderId);

        // 8. Resolve sender display info for the return DTO
        var (name, image) = await ResolveSenderDirectAsync(senderId, ct);

        return ServiceResult<MessageDto>.Success(new MessageDto
        {
            MessageId = message.MessageId,
            ConversationId = message.ConversationId,
            SenderId = senderId,
            SenderFullName = name,
            SenderImageUrl = image,
            MessageText = message.MessageText,
            ImageUrl = message.ImageUrl,
            IsRead = false,
            SentAt = message.SentAt
        });
    }

    // ── MarkMessagesReadAsync ─────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<long>>> MarkMessagesReadAsync(
        string userId,
        int conversationId,
        CancellationToken ct = default)
    {
        // Get IDs of messages that will be marked (for hub relay)
        var unreadMessages = await _uow.Repository<ChatMessage>()
            .GetAllAsync(
                m => m.ConversationId == conversationId &&
                     m.SenderId != userId &&
                     !m.IsRead,
                ct);

        var ids = unreadMessages.Select(m => m.MessageId).ToList();

        if (!ids.Any())
            return ServiceResult<IReadOnlyList<long>>.Success(ids);

        await _uow.Chat.MarkMessagesAsReadAsync(conversationId, userId, ct);

        return ServiceResult<IReadOnlyList<long>>.Success(ids);
    }

    // ── GetTotalUnreadCountAsync ──────────────────────────────────────────────

    public async Task<ServiceResult<int>> GetTotalUnreadCountAsync(
        string userId,
        CancellationToken ct = default)
    {
        var count = await _uow.Chat.GetUnreadCountAsync(userId, ct);
        return ServiceResult<int>.Success(count);
    }

    // ── GetConversationUnreadCountAsync ───────────────────────────────────────

    public async Task<ServiceResult<int>> GetConversationUnreadCountAsync(
        string userId,
        int conversationId,
        CancellationToken ct = default)
    {
        var count = await _uow.Repository<ChatMessage>()
            .CountAsync(m => m.ConversationId == conversationId &&
                             m.SenderId != userId &&
                             !m.IsRead, ct);

        return ServiceResult<int>.Success(count);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<ConversationDto> BuildConversationDtoAsync(
        ChatConversation conv,
        string viewerId,
        CancellationToken ct)
    {
        var lastMsg = conv.Messages
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefault();

        var unread = conv.Messages
            .Count(m => m.SenderId != viewerId && !m.IsRead);

        bool viewerIsStudent = conv.StudentId == viewerId;
        var otherPartyId = viewerIsStudent ? conv.OwnerId : conv.StudentId;

        string otherName;
        string? otherImage;

        if (viewerIsStudent)
        {
            var owner = conv.Owner
                ?? await _uow.Owners.GetByUserIdAsync(conv.OwnerId, ct);
            otherName = owner?.FullName ?? "Owner";
            otherImage = owner?.ProfileImageUrl;
        }
        else
        {
            var student = conv.Student
                ?? await _uow.Students.GetByUserIdAsync(conv.StudentId, ct);
            otherName = student?.FullName ?? "Student";
            otherImage = student?.ProfileImageUrl;
        }

        return new ConversationDto
        {
            ConversationId = conv.Id,
            StudentId = conv.StudentId,
            StudentFullName = conv.Student?.FullName ?? string.Empty,
            StudentImageUrl = conv.Student?.ProfileImageUrl,
            OwnerId = conv.OwnerId,
            OwnerFullName = conv.Owner?.FullName ?? string.Empty,
            OwnerImageUrl = conv.Owner?.ProfileImageUrl,
            PropertyId = conv.PropertyId,
            PropertyTitle = conv.Property?.Title,
            PropertyImageUrl = conv.Property?.Images
                                      .FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            LastMessageText = lastMsg?.MessageText,
            LastMessageImageUrl = lastMsg?.ImageUrl,
            LastMessageAt = conv.LastMessageAt,
            OtherPartyId = otherPartyId,
            OtherPartyFullName = otherName,
            OtherPartyImageUrl = otherImage,
            UnreadCount = unread,
            IsOnline = false  // populated by hub/controller from ConnectionTracker
        };
    }

    private async Task<(string Name, string? Image)> ResolveSenderAsync(
        string senderId,
        Dictionary<string, (string Name, string? Image)> studentCache,
        Dictionary<string, (string Name, string? Image)> ownerCache,
        CancellationToken ct)
    {
        if (studentCache.TryGetValue(senderId, out var sc)) return sc;
        if (ownerCache.TryGetValue(senderId, out var oc)) return oc;

        var student = await _uow.Students.GetByUserIdAsync(senderId, ct);
        if (student is not null)
        {
            var result = (student.FullName, student.ProfileImageUrl);
            studentCache[senderId] = result;
            return result;
        }

        var owner = await _uow.Owners.GetByUserIdAsync(senderId, ct);
        if (owner is not null)
        {
            var result = (owner.FullName, owner.ProfileImageUrl);
            ownerCache[senderId] = result;
            return result;
        }

        return ("Unknown", null);
    }

    private async Task<(string Name, string? Image)> ResolveSenderDirectAsync(
        string senderId,
        CancellationToken ct)
    {
        var student = await _uow.Students.GetByUserIdAsync(senderId, ct);
        if (student is not null) return (student.FullName, student.ProfileImageUrl);

        var owner = await _uow.Owners.GetByUserIdAsync(senderId, ct);
        if (owner is not null) return (owner.FullName, owner.ProfileImageUrl);

        return ("Unknown", null);
    }
}