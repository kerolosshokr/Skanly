// Skanly.Application/Features/Verification/DTOs/OcrResultDto.cs
namespace Skanly.Application.Features.Verification.DTOs;

/// <summary>
/// Raw result returned by IOcrService after processing an image.
/// Contains extracted text fields + confidence score.
/// Never stored directly — mapped to IdentityVerification entity fields.
/// </summary>
public class OcrResultDto
{
    public bool IsSuccess { get; init; }
    public string? FailureReason { get; init; }

    // Extracted fields (null if not found with sufficient confidence)
    public string? ExtractedName { get; init; }
    public string? ExtractedNationalId { get; init; }
    public DateOnly? ExtractedBirthDate { get; init; }
    public string? ExtractedGender { get; init; }

    // Quality indicators
    public double ConfidenceScore { get; init; }
    public string? RawText { get; init; }

    // Validation flags
    public bool NationalIdFormatValid =>
        !string.IsNullOrEmpty(ExtractedNationalId) &&
        ExtractedNationalId.Length == 14 &&
        ExtractedNationalId.All(char.IsDigit);

    public bool IsAboveMinConfidence(double minScore)
        => ConfidenceScore >= minScore;

    public static OcrResultDto Failure(string reason) => new()
    {
        IsSuccess = false,
        FailureReason = reason
    };

    public static OcrResultDto Success(
        string? name,
        string? nationalId,
        DateOnly? birthDate,
        string? gender,
        double confidence,
        string? rawText) => new()
        {
            IsSuccess = true,
            ExtractedName = name?.Trim(),
            ExtractedNationalId = nationalId?.Trim(),
            ExtractedBirthDate = birthDate,
            ExtractedGender = gender?.Trim(),
            ConfidenceScore = confidence,
            RawText = rawText
        };
}