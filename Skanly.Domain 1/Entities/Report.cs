// Skanly.Domain/Entities/Report.cs
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
using Skanly.Domain.Interfaces;
using Skanly.Domain_1.Enums;
using System.ComponentModel.DataAnnotations;

namespace Skanly.Domain.Entities;

public class Report : BaseEntity<int>, IAggregateRoot
{
    [Required]
    public string ReporterId { get; set; } = string.Empty;

    public int? ReportedPropertyId { get; set; }

    public string? ReportedUserId { get; set; }

    public ReportType ReportType { get; set; }

    [Required, MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public string? ResolvedByAdminId { get; set; }

    [MaxLength(1000)]
    public string? Resolution { get; set; }

    public DateTime? ResolvedAt { get; set; }

    // Navigation
    public Property? ReportedProperty { get; set; }
    public Admin? ResolvedByAdmin { get; set; }
}