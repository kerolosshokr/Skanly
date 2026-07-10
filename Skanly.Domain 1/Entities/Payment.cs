// Skanly.Domain/Entities/Payment.cs
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
using Skanly.Domain.Interfaces;
using Skanly.Domain_1.Enums;
using System.ComponentModel.DataAnnotations;

namespace Skanly.Domain.Entities;

public class Payment : BaseEntity<int>, IAggregateRoot
{
    [Required]
    public int BookingId { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(100)]
    public string? TransactionReference { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime? PaidAt { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
}