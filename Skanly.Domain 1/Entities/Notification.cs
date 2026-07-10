using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
namespace Skanly.Domain.Entities
{
    public  class Notification : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;
    }
}

