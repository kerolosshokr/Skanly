using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities
{
    public  class ChatConversation : BaseEntity
    {
        public string StudentId { get; set; } = string.Empty;

        public string OwnerId { get; set; } = string.Empty;

        public Guid PropertyId { get; set; }

        public Property Property { get; set; } = null!;

        public ICollection<ChatMessage> Messages { get; set; }
            = new List<ChatMessage>();
    }
}
