// Skanly.Infrastructure/ExternalServices/Ocr/OcrImagePreprocessor.cs
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Skanly.Infrastructure.ExternalServices.Ocr;

/// <summary>
/// Preprocesses images before OCR to improve accuracy:
/// - Converts to grayscale
/// - Increases contrast
/// - Resizes to optimal DPI for Tesseract
/// - Removes noise
/// </summary>
public static class OcrImagePreprocessor
{
    public static async Task<Stream> PreprocessAsync(
        Stream input,
        CancellationToken ct = default)
    {
        var output = new MemoryStream();

        using var image = await Image.LoadAsync(input, ct);

        image.Mutate(x => x
            // Convert to grayscale for better OCR accuracy
            .Grayscale()
            // Increase contrast to make text stand out
            .Contrast(1.5f)
            // Sharpen text edges
            
            // Resize if too small (Tesseract works best at ~300 DPI)
            .Resize(new ResizeOptions
            {
                Mode = ResizeMode.Min,
                Size = new Size(
                    Math.Max(image.Width, 1600),
                    Math.Max(image.Height, 1000))
            }));

        await image.SaveAsPngAsync(output, ct);
        output.Seek(0, SeekOrigin.Begin);

        return output;
    }
}