// Skanly.Domain/Entities/Contract.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Contract : BaseEntity<int>, IAggregateRoot
{
    [Required]
    public int BookingId { get; set; }

    [Required, MaxLength(50)]
    public string ContractNumber { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string PdfUrl { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Booking Booking { get; set; } = null!;
}