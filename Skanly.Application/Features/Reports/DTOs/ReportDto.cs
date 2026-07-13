// Skanly.Application/Features/Reports/DTOs/ReportDto.cs
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Features.Reports.DTOs;

public class ReportDto
{
    public int ReportId { get; init; }

    // Reporter info
    public string ReporterId { get; init; } = string.Empty;
    public string ReporterFullName { get; init; } = string.Empty;
    public string? ReporterImageUrl { get; init; }

    // Target — one of these will be populated
    public int? ReportedPropertyId { get; init; }
    public string? ReportedPropertyTitle { get; init; }
    public string? ReportedPropertyImageUrl { get; init; }
    public string? ReportedUserId { get; init; }
    public string? ReportedUserFullName { get; init; }
    public string? ReportedUserImageUrl { get; init; }

    // Report details
    public ReportType ReportType { get; init; }
    public string ReportTypeDisplay => ReportType switch
    {
        ReportType.FakeListing => "Fake Listing",
        ReportType.FraudulentOwner => "Fraudulent Owner",
        ReportType.InappropriateContent => "Inappropriate Content",
        ReportType.PropertyIssue => "Property Issue",
        _ => ReportType.ToString()
    };
    public string ReportTypeIcon => ReportType switch
    {
        ReportType.FakeListing => "fa-home text-warning",
        ReportType.FraudulentOwner => "fa-user-slash text-danger",
        ReportType.InappropriateContent => "fa-ban text-danger",
        ReportType.PropertyIssue => "fa-wrench text-info",
        _ => "fa-flag text-secondary"
    };

    public string Description { get; init; } = string.Empty;
    public string? EvidenceUrl { get; init; }

    // Status
    public ReportStatus Status { get; init; }
    public string StatusDisplay => Status switch
    {
        ReportStatus.Open => "Open",
        ReportStatus.UnderInvestigation => "Under Investigation",
        ReportStatus.Resolved => "Resolved",
        ReportStatus.Dismissed => "Dismissed",
        _ => Status.ToString()
    };
    public string StatusBadgeClass => Status switch
    {
        ReportStatus.Open => "bg-danger",
        ReportStatus.UnderInvestigation => "bg-warning text-dark",
        ReportStatus.Resolved => "bg-success",
        ReportStatus.Dismissed => "bg-secondary",
        _ => "bg-secondary"
    };

    // Resolution
    public string? ResolvedByAdminId { get; init; }
    public string? ResolvedByAdminName { get; init; }
    public string? Resolution { get; init; }
    public DateTime? ResolvedAt { get; init; }

    // Timestamps
    public DateTime CreatedAt { get; init; }
    public string TimeAgo => GetTimeAgo(CreatedAt);

    public bool IsOpen =>
        Status == ReportStatus.Open ||
        Status == ReportStatus.UnderInvestigation;

    private static string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        return span.TotalMinutes < 1 ? "Just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays}d ago"
            : dt.ToString("MMM dd, yyyy");
    }
}