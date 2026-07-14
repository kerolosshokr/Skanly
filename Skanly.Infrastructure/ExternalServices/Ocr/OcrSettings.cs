// Skanly.Infrastructure/ExternalServices/Ocr/OcrSettings.cs
namespace Skanly.Infrastructure.ExternalServices.Ocr;

public class OcrSettings
{
    public const string SectionName = "OcrSettings";

    public string Provider { get; init; } = "Tesseract";
    public string TesseractDataPath { get; init; } = "./tessdata";
    public string TesseractLanguages { get; init; } = "ara+eng";
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
    public string[] AllowedExtensions { get; init; }
        = { ".jpg", ".jpeg", ".png", ".pdf" };
    public bool EnablePreprocessing { get; init; } = true;
    public double MinConfidenceScore { get; init; } = 60.0;
}