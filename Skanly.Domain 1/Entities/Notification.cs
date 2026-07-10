// Skanly.Domain/Entities/Notification.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Enums;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Notification : IAggregateRoot
{
    public long NotificationId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public bool IsRead { get; set; }

    public int? RelatedEntityId { get; set; }

    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}