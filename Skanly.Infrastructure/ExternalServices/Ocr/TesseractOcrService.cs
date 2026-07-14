// Skanly.Infrastructure/ExternalServices/Ocr/TesseractOcrService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skanly.Application.Features.Verification.DTOs;
using Skanly.Application.Features.Verification.Interfaces;
using System.Text.RegularExpressions;
using Tesseract;

namespace Skanly.Infrastructure.ExternalServices.Ocr;

/// <summary>
/// Phase 1 OCR implementation using Tesseract.NET.
///
/// To swap to Azure Form Recognizer (Phase 2):
///   1. Create AzureFormRecognizerOcrService : IOcrService
///   2. Register it instead of TesseractOcrService in DI
///   3. Zero changes to Application or Web layers
///
/// Tesseract NuGet: Tesseract (v5.x)
/// tessdata files: download from https://github.com/tesseract-ocr/tessdata
/// Required: ara.traineddata + eng.traineddata in tessdata folder
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly OcrSettings _settings;
    private readonly ILogger<TesseractOcrService> _logger;

    // Egyptian National ID regex pattern
    // Format: 14 digits — CYYMMDDGGGSSSC
    // C = Century (2=1900s, 3=2000s)
    // YY = Year, MM = Month, DD = Day
    // GGG = Governorate code
    // SSS = Sequence number
    // C = Check digit
    private static readonly Regex NationalIdRegex =
        new(@"\b([23]\d{13})\b", RegexOptions.Compiled);

    // Arabic name pattern (Arabic Unicode range)
    private static readonly Regex ArabicNameRegex =
        new(@"[\u0600-\u06FF]{2,}(?:\s+[\u0600-\u06FF]{2,})+",
            RegexOptions.Compiled);

    // English name pattern (fallback)
    private static readonly Regex EnglishNameRegex =
        new(@"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+){1,4}\b",
            RegexOptions.Compiled);

    // Date patterns
    private static readonly Regex[] DatePatterns =
    {
        new(@"\b(\d{2})[/\-\.](\d{2})[/\-\.](\d{4})\b"),  // DD/MM/YYYY
        new(@"\b(\d{4})[/\-\.](\d{2})[/\-\.](\d{2})\b"),  // YYYY/MM/DD
    };

    public TesseractOcrService(
        IOptions<OcrSettings> settings,
        ILogger<TesseractOcrService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    // ── ExtractFromImageAsync ─────────────────────────────────────────────────

    public async Task<OcrResultDto> ExtractFromImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Preprocess image for better OCR accuracy
            Stream processedStream = imageStream;
            if (_settings.EnablePreprocessing)
            {
                processedStream = await OcrImagePreprocessor
                    .PreprocessAsync(imageStream, ct);
            }

            // 2. Convert stream to byte array for Tesseract
            using var ms = new MemoryStream();
            await processedStream.CopyToAsync(ms, ct);
            var imageBytes = ms.ToArray();

            // 3. Run Tesseract OCR
            string rawText;
            float confidence;

            using var engine = new TesseractEngine(
                _settings.TesseractDataPath,
                _settings.TesseractLanguages,
                EngineMode.Default);

            engine.SetVariable("tessedit_char_whitelist", string.Empty);
            engine.SetVariable("preserve_interword_spaces", "1");

            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);

            rawText = page.GetText();
            confidence = page.GetMeanConfidence() * 100;

            _logger.LogInformation(
                "Tesseract OCR completed. Confidence={Confidence:F1}% " +
                "Text length={Length}",
                confidence, rawText.Length);

            if (string.IsNullOrWhiteSpace(rawText))
                return OcrResultDto.Failure(
                    "No text could be extracted from the image. " +
                    "Please upload a clearer photo.");

            // 4. Extract structured fields from raw text
            var nationalId = ExtractNationalId(rawText);
            var name = ExtractName(rawText);
            var birthDate = ExtractBirthDate(rawText, nationalId);
            var gender = ExtractGender(rawText, nationalId);

            return OcrResultDto.Success(
                name, nationalId, birthDate, gender,
                Math.Round(confidence, 2), rawText);
        }
        catch (TesseractException tex)
        {
            _logger.LogError(tex,
                "Tesseract engine error processing {FileName}", fileName);
            return OcrResultDto.Failure(
                "OCR processing failed. Please ensure tessdata files " +
                "are installed and the image is clear.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected OCR error for {FileName}", fileName);
            return OcrResultDto.Failure(
                "An unexpected error occurred during document processing.");
        }
    }

    // ── ValidateEgyptianId ────────────────────────────────────────────────────

    public EgyptianIdValidationResult ValidateEgyptianId(string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return EgyptianIdValidationResult.Invalid("National ID is empty.");

        var clean = nationalId.Trim().Replace(" ", "");

        if (clean.Length != 14)
            return EgyptianIdValidationResult.Invalid(
                "Egyptian National ID must be exactly 14 digits.");

        if (!clean.All(char.IsDigit))
            return EgyptianIdValidationResult.Invalid(
                "National ID must contain only digits.");

        // Parse century digit
        var centuryDigit = int.Parse(clean[0].ToString());
        if (centuryDigit != 2 && centuryDigit != 3)
            return EgyptianIdValidationResult.Invalid(
                "Invalid century digit. Must be 2 (born 1900s) " +
                "or 3 (born 2000s).");

        // Parse birth date
        var year = (centuryDigit == 2 ? 1900 : 2000) +
                    int.Parse(clean.Substring(1, 2));
        var month = int.Parse(clean.Substring(3, 2));
        var day = int.Parse(clean.Substring(5, 2));

        if (month < 1 || month > 12 || day < 1 || day > 31)
            return EgyptianIdValidationResult.Invalid(
                "Invalid birth date encoded in National ID.");

        DateOnly birthDate;
        try
        {
            birthDate = new DateOnly(year, month, day);
        }
        catch
        {
            return EgyptianIdValidationResult.Invalid(
                "Birth date in National ID is not a valid calendar date.");
        }

        // Must be at least 16 years old
        var minBirthDate = DateOnly.FromDateTime(
            DateTime.Today.AddYears(-16));
        if (birthDate > minBirthDate)
            return EgyptianIdValidationResult.Invalid(
                "You must be at least 16 years old.");

        // Governorate code (positions 7-8)
        var govCode = clean.Substring(7, 2);
        var validGovCodes = new HashSet<string>
        {
            "01","02","03","04","11","12","13","14","15",
            "16","17","18","19","21","22","23","24","25",
            "26","27","28","29","31","32","33","34","35",
            "88"  // Born abroad
        };

        if (!validGovCodes.Contains(govCode))
            return EgyptianIdValidationResult.Invalid(
                $"Invalid governorate code: {govCode}.");

        // Gender from sequence number (positions 10-12, odd=Male, even=Female)
        var seqNum = int.Parse(clean.Substring(10, 3));
        var gender = seqNum % 2 != 0 ? "Male" : "Female";

        return EgyptianIdValidationResult.Valid(birthDate, gender, govCode);
    }

    // ── Private Extract Helpers ───────────────────────────────────────────────

    private static string? ExtractNationalId(string text)
    {
        var match = NationalIdRegex.Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractName(string text)
    {
        // Try Arabic name first (Egyptian IDs are primarily in Arabic)
        var arabicMatch = ArabicNameRegex.Match(text);
        if (arabicMatch.Success)
            return arabicMatch.Value.Trim();

        // Fallback to English
        var englishMatches = EnglishNameRegex.Matches(text);
        if (englishMatches.Count > 0)
        {
            // Return the longest match (most likely to be a full name)
            return englishMatches
                .OrderByDescending(m => m.Value.Length)
                .First().Value.Trim();
        }

        return null;
    }

    private static DateOnly? ExtractBirthDate(
        string text,
        string? nationalId)
    {
        // If we have a valid National ID, decode birth date from it
        if (!string.IsNullOrEmpty(nationalId) && nationalId.Length == 14)
        {
            var centuryDigit = int.Parse(nationalId[0].ToString());
            if (centuryDigit == 2 || centuryDigit == 3)
            {
                try
                {
                    var year = (centuryDigit == 2 ? 1900 : 2000) +
                                int.Parse(nationalId.Substring(1, 2));
                    var month = int.Parse(nationalId.Substring(3, 2));
                    var day = int.Parse(nationalId.Substring(5, 2));

                    if (month is >= 1 and <= 12 && day is >= 1 and <= 31)
                        return new DateOnly(year, month, day);
                }
                catch { /* fall through to text-based extraction */ }
            }
        }

        // Try to find date in raw text
        foreach (var pattern in DatePatterns)
        {
            var match = pattern.Match(text);
            if (!match.Success) continue;

            try
            {
                // Determine format from pattern
                if (pattern.ToString().StartsWith(@"\b(\d{4})"))
                {
                    // YYYY/MM/DD
                    return new DateOnly(
                        int.Parse(match.Groups[1].Value),
                        int.Parse(match.Groups[2].Value),
                        int.Parse(match.Groups[3].Value));
                }
                else
                {
                    // DD/MM/YYYY
                    return new DateOnly(
                        int.Parse(match.Groups[3].Value),
                        int.Parse(match.Groups[2].Value),
                        int.Parse(match.Groups[1].Value));
                }
            }
            catch { continue; }
        }

        return null;
    }

    private static string? ExtractGender(string text, string? nationalId)
    {
        // Decode from National ID sequence number
        if (!string.IsNullOrEmpty(nationalId) && nationalId.Length == 14)
        {
            try
            {
                var seqNum = int.Parse(nationalId.Substring(10, 3));
                return seqNum % 2 != 0 ? "Male" : "Female";
            }
            catch { }
        }

        // Arabic keywords
        if (text.Contains("ذكر") || text.Contains("Male", StringComparison.OrdinalIgnoreCase))
            return "Male";
        if (text.Contains("أنثى") || text.Contains("Female", StringComparison.OrdinalIgnoreCase))
            return "Female";

        return null;
    }
}