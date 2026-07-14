// Skanly.Application/Features/Verification/DTOs/VerificationDto.cs
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Verification.DTOs;

public class VerificationDto
{
    public int VerificationId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserFullName { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public string? UserImageUrl { get; init; }
    public string UserRole { get; init; } = string.Empty;

    // Uploaded files
    public string NationalIdFrontUrl { get; init; } = string.Empty;
    public string? NationalIdBackUrl { get; init; }

    // OCR extracted data
    public string? ExtractedName { get; init; }
    public string? ExtractedNationalId { get; init; }
    public DateOnly? ExtractedBirthDate { get; init; }
    public double? OcrConfidenceScore { get; init; }

    // Status
    public VerificationStatus Status { get; init; }
    public string StatusDisplay => Status switch
    {
        VerificationStatus.Pending => "Pending Review",
        VerificationStatus.Approved => "Approved",
        VerificationStatus.Rejected => "Rejected",
        _ => Status.ToString()
    };
    public string StatusBadgeClass => Status switch
    {
        VerificationStatus.Pending => "bg-warning text-dark",
        VerificationStatus.Approved => "bg-success",
        VerificationStatus.Rejected => "bg-danger",
        _ => "bg-secondary"
    };

    // Review details
    public string? ReviewedByAdminName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }

    public DateTime CreatedAt { get; init; }
    public string TimeAgo => GetTimeAgo(CreatedAt);

    // Validation helpers for Admin review UI
    public bool NationalIdFormatValid =>
        !string.IsNullOrEmpty(ExtractedNationalId) &&
        ExtractedNationalId.Length == 14 &&
        ExtractedNationalId.All(char.IsDigit);

    public bool IsHighConfidence =>
        OcrConfidenceScore.HasValue && OcrConfidenceScore.Value >= 75.0;

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