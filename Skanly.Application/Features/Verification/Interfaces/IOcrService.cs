// Skanly.Application/Features/Verification/Interfaces/IOcrService.cs
using Skanly.Application.Features.Verification.DTOs;

namespace Skanly.Application.Features.Verification.Interfaces;

/// <summary>
/// Abstraction over the OCR engine.
/// Application layer calls this — never Tesseract or Azure directly.
///
/// Phase 1: Tesseract.NET (Infrastructure implementation)
/// Phase 2: Azure Form Recognizer / AWS Textract
///          → swap Implementation class in DI, zero Application changes
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Processes an image file stream and extracts National ID fields.
    /// Must never throw — all errors captured in OcrResultDto.IsSuccess.
    /// </summary>
    Task<OcrResultDto> ExtractFromImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken ct = default);

    /// <summary>
    /// Validates that an extracted National ID number is structurally
    /// valid for an Egyptian National ID (14 digits, birth date encoded).
    /// Pure logic — no API call needed.
    /// </summary>
    EgyptianIdValidationResult ValidateEgyptianId(string nationalId);
}

public class EgyptianIdValidationResult
{
    public bool IsValid { get; init; }
    public string? Reason { get; init; }
    public DateOnly? EncodedBirthDate { get; init; }
    public string? EncodedGender { get; init; }  // "Male" | "Female"
    public string? GovernorateCode { get; init; }

    public static EgyptianIdValidationResult Valid(
        DateOnly birthDate,
        string gender,
        string govCode) => new()
        {
            IsValid = true,
            EncodedBirthDate = birthDate,
            EncodedGender = gender,
            GovernorateCode = govCode
        };

    public static EgyptianIdValidationResult Invalid(string reason) => new()
    {
        IsValid = false,
        Reason = reason
    };
}