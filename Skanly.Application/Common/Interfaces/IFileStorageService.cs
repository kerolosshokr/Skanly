// Skanly.Application/Common/Interfaces/IFileStorageService.cs
using Microsoft.AspNetCore.Http;

namespace Skanly.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>Saves a file and returns its relative URL path.</summary>
    Task<string> SaveAsync(
        IFormFile file,
        string folder,
        CancellationToken ct = default);

    Task DeleteAsync(string relativePath, CancellationToken ct = default);

    bool IsImageFile(IFormFile file);
}