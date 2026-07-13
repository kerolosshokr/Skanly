using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Skanly.Application.Features.Chat.DTOs;
using Skanly.Application.Features.Chat.Interfaces;
using Skanly.Infrastructure.RealTime;
using System.Security.Claims;

namespace Skanly.Infrastructure.RealTime.Hubs
{
    /// <summary>
    /// Real-time chat hub.
    ///
    /// Client method contract (JavaScript side must implement these):
    ///   ReceiveMessage(MessageDto)         — new message arrived
    ///   MessageRead(long[] messageIds)     — recipient read your messages
    ///   UserOnline(string userId)          — a contact came online
    ///   UserOffline(string userId)         — a contact went offline
    ///   TypingStarted(string userId, int conversationId)
    ///   TypingStopped(string userId, int conversationId)
    ///   Error(string message)              — server-side error relay
    ///
    /// Server methods (client calls Hub):
    ///   SendMessage(SendMessageDto)
    ///   SendImageMessage(int conversationId, string imageBase64, string extension)
    ///   MarkRead(int conversationId)
    ///   StartTyping(int conversationId, string recipientId)
    ///   StopTyping(int conversationId, string recipientId)
    ///   JoinConversation(int conversationId)
    ///   LeaveConversation(int conversationId)
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ConnectionTracker _tracker;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService,
            ConnectionTracker tracker,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _tracker = tracker;
            _logger = logger;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _tracker.Add(userId, Context.ConnectionId);

            // Notify contacts that this user is now online
            await NotifyContactsOnlineStatusAsync(userId, online: true);

            _logger.LogInformation(
                "User {UserId} connected. ConnectionId={Conn}",
                userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _tracker.Remove(userId, Context.ConnectionId);

            // Only broadcast offline if user has NO remaining connections
            if (!_tracker.IsOnline(userId))
                await NotifyContactsOnlineStatusAsync(userId, online: false);

            _logger.LogInformation(
                "User {UserId} disconnected. ConnectionId={Conn}",
                userId, Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        // ── Join / Leave Conversation Groups ──────────────────────────────────────

        /// <summary>
        /// Client joins the SignalR group for a conversation so it receives
        /// all real-time events (new messages, read receipts, typing) for it.
        /// </summary>
        public async Task JoinConversation(int conversationId)
        {
            var userId = GetUserId();
            var groupName = ConversationGroup(conversationId);

            // Verify membership before adding to group
            var result = await _chatService
                .GetConversationByIdAsync(userId, conversationId);

            if (!result.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", "Access denied to conversation.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Mark unread as read when joining
            await MarkReadInternal(userId, conversationId);
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId, ConversationGroup(conversationId));
        }

        // ── Send Text Message ─────────────────────────────────────────────────────

        public async Task SendMessage(int conversationId, string messageText)
        {
            var userId = GetUserId();

            var dto = new SendMessageDto
            {
                ConversationId = conversationId,
                MessageText = messageText
            };

            var result = await _chatService.SendMessageAsync(userId, dto);

            if (!result.IsSuccess)
            {
                await Clients.Caller.SendAsync(
                    "Error", result.ErrorMessage ?? "Failed to send message.");
                return;
            }

            var message = result.Data!;

            // Broadcast to everyone in the conversation group (including sender)
            await Clients
                .Group(ConversationGroup(conversationId))
                .SendAsync("ReceiveMessage", message);

            _logger.LogDebug(
                "Message {MsgId} broadcast to group {Group}",
                message.MessageId, ConversationGroup(conversationId));
        }

        // ── Send Image Message ────────────────────────────────────────────────────

        /// <summary>
        /// Client sends a base64-encoded image.
        /// Hub decodes → saves via IFileStorageService → persists message.
        ///
        /// Limitation: images over ~3 MB should use the HTTP endpoint instead
        /// (SignalR messages have a 32 KB default — increase MaximumReceiveMessageSize
        /// in Program.cs if using this approach, or use the AJAX endpoint).
        /// </summary>
        public async Task SendImageMessage(
            int conversationId,
            string imageBase64,
            string extension)
        {
            var userId = GetUserId();

            // Validate extension
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = extension.ToLowerInvariant().TrimStart('.');
            if (!allowed.Contains($".{ext}"))
            {
                await Clients.Caller.SendAsync(
                    "Error", "Invalid image type.");
                return;
            }

            // Decode base64 → IFormFile substitute (MemoryStream)
            byte[] imageBytes;
            try { imageBytes = Convert.FromBase64String(imageBase64); }
            catch
            {
                await Clients.Caller.SendAsync("Error", "Invalid image data.");
                return;
            }

            if (imageBytes.Length > 5 * 1024 * 1024)
            {
                await Clients.Caller.SendAsync("Error", "Image exceeds 5 MB.");
                return;
            }

            // Save to disk via a MemoryFormFile wrapper
            var formFile = new MemoryFormFile(imageBytes, $"chat_image.{ext}");
            var dto = new SendMessageDto
            {
                ConversationId = conversationId,
                Image = formFile
            };

            var result = await _chatService.SendMessageAsync(userId, dto);

            if (!result.IsSuccess)
            {
                await Clients.Caller.SendAsync(
                    "Error", result.ErrorMessage ?? "Failed to send image.");
                return;
            }

            await Clients
                .Group(ConversationGroup(conversationId))
                .SendAsync("ReceiveMessage", result.Data);
        }

        // ── Read Receipts ─────────────────────────────────────────────────────────

        public async Task MarkRead(int conversationId)
        {
            var userId = GetUserId();
            await MarkReadInternal(userId, conversationId);
        }

        private async Task MarkReadInternal(string userId, int conversationId)
        {
            var result = await _chatService
                .MarkMessagesReadAsync(userId, conversationId);

            if (!result.IsSuccess || !result.Data!.Any()) return;

            // Notify everyone in the group (specifically the sender(s))
            // that their messages have been read
            await Clients
                .Group(ConversationGroup(conversationId))
                .SendAsync("MessageRead", new
                {
                    readByUserId = userId,
                    conversationId,
                    messageIds = result.Data
                });
        }

        // ── Typing Indicators ─────────────────────────────────────────────────────

        /// <summary>
        /// Relay typing started event to the recipient only.
        /// Never persisted — purely ephemeral.
        /// </summary>
        public async Task StartTyping(int conversationId, string recipientId)
        {
            var connections = _tracker.GetConnectionIds(recipientId);
            if (!connections.Any()) return;

            await Clients.Clients(connections).SendAsync("TypingStarted", new
            {
                userId = GetUserId(),
                conversationId
            });
        }

        public async Task StopTyping(int conversationId, string recipientId)
        {
            var connections = _tracker.GetConnectionIds(recipientId);
            if (!connections.Any()) return;

            await Clients.Clients(connections).SendAsync("TypingStopped", new
            {
                userId = GetUserId(),
                conversationId
            });
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private string GetUserId()
            => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new HubException("Unauthenticated.");

        private static string ConversationGroup(int conversationId)
            => $"conversation_{conversationId}";

        private async Task NotifyContactsOnlineStatusAsync(
            string userId, bool online)
        {
            // Get all conversations for this user to find their contacts
            var result = await _chatService.GetConversationsAsync(userId);
            if (!result.IsSuccess) return;

            var contactIds = result.Data!
                .Select(c => c.OtherPartyId)
                .Distinct()
                .ToList();

            foreach (var contactId in contactIds)
            {
                var contactConnections = _tracker.GetConnectionIds(contactId);
                if (!contactConnections.Any()) continue;

                var eventName = online ? "UserOnline" : "UserOffline";
                await Clients.Clients(contactConnections)
                    .SendAsync(eventName, userId);
            }
        }
    }

    /// <summary>
    /// IFormFile wrapper for in-memory byte arrays (used for Hub image upload).
    /// </summary>
    internal sealed class MemoryFormFile : IFormFile
    {
        private readonly byte[] _data;
        public MemoryFormFile(byte[] data, string fileName)
        {
            _data = data;
            FileName = fileName;
            ContentType = GetContentType(fileName);
            Name = "file";
            Headers = new HeaderDictionary();
        }

        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers { get; }
        public long Length => _data.Length;
        public string Name { get; }
        public string FileName { get; }
        public Stream OpenReadStream() => new MemoryStream(_data);
        public void CopyTo(Stream target) => target.Write(_data, 0, _data.Length);
        public Task CopyToAsync(Stream target, CancellationToken ct = default)
            => target.WriteAsync(_data, 0, _data.Length, ct);

        private static string GetContentType(string fileName)
            => Path.GetExtension(fileName).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
    }
}
