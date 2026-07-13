// Skanly.Domain/Entities/Review.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Review : BaseEntity<int>, IAggregateRoot
{
    [Required]
    public int BookingId { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public int PropertyId { get; set; }
    // Skanly.Domain/Entities/Review.cs  — add this property
    public bool IsHidden { get; set; } = false;

    [Range(1, 5)] public byte CleanlinessRating { get; set; }
    [Range(1, 5)] public byte SafetyRating { get; set; }
    [Range(1, 5)] public byte InternetRating { get; set; }
    [Range(1, 5)] public byte LocationRating { get; set; }
    [Range(1, 5)] public byte QuietnessRating { get; set; }
    [Range(1, 5)] public byte OverallRating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public Property Property { get; set; } = null!;
}