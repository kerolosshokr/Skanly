// Skanly.Domain/Entities/Admin.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Admin : IAggregateRoot
{
    [Key]
    public string UserId { get; set; } = string.Empty;   // FK to AspNetUsers.Id

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation (reverse — entities this admin has actioned)
    public ICollection<IdentityVerification> ReviewedVerifications { get; set; } = new List<IdentityVerification>();
    public ICollection<Report> ResolvedReports { get; set; } = new List<Report>();
    public ICollection<CommissionSetting> CommissionSettings { get; set; } = new List<CommissionSetting>();

    public string FullName => $"{FirstName} {LastName}";
}