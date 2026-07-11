// Skanly.Infrastructure/FileStorage/LocalFileStorageService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Skanly.Application.Common.Interfaces;

namespace Skanly.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] ImageExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveAsync(
        IFormFile file,
        string folder,
        CancellationToken ct = default)
    {
        // Sanitize folder path and generate a unique filename
        var safeFolder = Path.Combine("uploads", folder.Trim('/'));
        var uploadsPath = Path.Combine(_env.WebRootPath, safeFolder);

        if (!Directory.Exists(uploadsPath))
            Directory.CreateDirectory(uploadsPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        // Return URL-style relative path for use in <img src="...">
        return $"/{safeFolder}/{fileName}".Replace("\\", "/");
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(relativePath)) return Task.CompletedTask;

        var fullPath = Path.Combine(
            _env.WebRootPath,
            relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public bool IsImageFile(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ImageExtensions.Contains(ext);
    }
}