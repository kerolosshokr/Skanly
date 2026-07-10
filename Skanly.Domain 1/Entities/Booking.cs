// Skanly.Domain/Entities/Booking.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Booking : BaseEntity<int>, IAggregateRoot
{
    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public int PropertyId { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    public DateOnly? CheckOutDate { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DepositAmount { get; set; }

    [Range(0, 100)]
    public decimal CommissionRate { get; set; } = 10.00m;

    public decimal? CommissionAmount { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
    public Property Property { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Contract? Contract { get; set; }
    public Review? Review { get; set; }
}