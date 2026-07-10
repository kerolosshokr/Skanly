// Skanly.Domain/Entities/IdentityVerification.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class IdentityVerification : BaseEntity<int>, IAggregateRoot
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string NationalIdFrontUrl { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? NationalIdBackUrl { get; set; }

    [MaxLength(150)]
    public string? ExtractedName { get; set; }

    [MaxLength(20)]
    public string? ExtractedNationalId { get; set; }

    public DateOnly? ExtractedBirthDate { get; set; }

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    public string? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(300)]
    public string? RejectionReason { get; set; }

    // Navigation
    public Admin? ReviewedByAdmin { get; set; }
}