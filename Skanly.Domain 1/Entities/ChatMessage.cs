using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities
{
    public  class ChatMessage : BaseEntity
    {
        public Guid ConversationId { get; set; }

        public string SenderId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; } = false;

        public ChatConversation Conversation { get; set; } = null!;
    }
}
