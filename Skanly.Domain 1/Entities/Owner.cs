// Skanly.Domain/Entities/Owner.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Owner : IAggregateRoot
{
    [Key]
    public string UserId { get; set; } = string.Empty;   // FK to AspNetUsers.Id

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(20)]
    public string? NationalId { get; set; }

    [MaxLength(150)]
    public string? BusinessName { get; set; }

    [MaxLength(300)]
    public string? ProfileImageUrl { get; set; }

    public bool IsIdentityVerified { get; set; }

    [MaxLength(300)]
    public string? BankAccountInfo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();

    public string FullName => $"{FirstName} {LastName}";
}