// Skanly.Infrastructure/ExternalServices/Ocr/AzureFormRecognizerOcrService.cs
// (stub — implement when upgrading to Phase 2)

using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Skanly.Application.Features.Verification.DTOs;
using Skanly.Application.Features.Verification.Interfaces;

namespace Skanly.Infrastructure.ExternalServices.Ocr;

/// <summary>
/// Phase 2: Azure Form Recognizer implementation.
///
/// To activate:
///   Replace in DI: services.AddScoped<IOcrService, AzureFormRecognizerOcrService>()
///   Zero changes in Application or Web layers.
///
/// NuGet: Azure.AI.FormRecognizer
/// </summary>
public class AzureFormRecognizerOcrService : IOcrService
{
    private readonly DocumentAnalysisClient _client;

    public AzureFormRecognizerOcrService(string endpoint, string apiKey)
    {
        _client = new DocumentAnalysisClient(
            new Uri(endpoint),
            new Azure.AzureKeyCredential(apiKey));
    }

    public async Task<OcrResultDto> ExtractFromImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken ct = default)
    {
        try
        {
            // Use prebuilt-idDocument model for National IDs
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-idDocument",
                imageStream,
                cancellationToken: ct);

            var result = operation.Value;
            if (!result.Documents.Any())
                return OcrResultDto.Failure("No ID document detected.");

            var doc = result.Documents[0];
            var fields = doc.Fields;

            string? name = null;
            string? nationalId = null;

            if (fields.TryGetValue("FirstName", out var fn) &&
                fields.TryGetValue("LastName", out var ln))
                name = $"{fn.Content} {ln.Content}".Trim();

            if (fields.TryGetValue("DocumentNumber", out var dn))
                nationalId = dn.Content;

            DateOnly? birthDate = null;
            if (fields.TryGetValue("DateOfBirth", out var dob) &&
                DateOnly.TryParse(dob.Content, out var parsedDate))
                birthDate = parsedDate;

            return OcrResultDto.Success(
                name, nationalId, birthDate, null,
                doc.Confidence * 100, null);
        }
        catch (Exception ex)
        {
            return OcrResultDto.Failure($"Azure OCR error: {ex.Message}");
        }
    }

    public EgyptianIdValidationResult ValidateEgyptianId(string nationalId)
    {
        // Reuse same logic — validation is pure business logic
        var tesseract = new TesseractOcrService(null!, null!);
        return tesseract.ValidateEgyptianId(nationalId);
    }
}