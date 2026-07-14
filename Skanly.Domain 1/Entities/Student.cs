// Skanly.Domain/Entities/Student.cs
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
using Skanly.Domain.Interfaces;
using Skanly.Domain_1.Enums;
using System.ComponentModel.DataAnnotations;

namespace Skanly.Domain.Entities;

public class Student : IAggregateRoot
{
    [Key]
    public string UserId { get; set; } = string.Empty;   // FK to AspNetUsers.Id

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    public DateOnly? BirthDate { get; set; }

    [MaxLength(20)]
    public string? NationalId { get; set; }

    public int? UniversityId { get; set; }

    [MaxLength(300)]
    public string? ProfileImageUrl { get; set; }
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool IsIdentityVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public University? University { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();

    public string FullName => $"{FirstName} {LastName}";
}