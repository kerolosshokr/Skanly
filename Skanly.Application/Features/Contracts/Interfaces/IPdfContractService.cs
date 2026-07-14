// Skanly.Application/Features/Contracts/Interfaces/IPdfContractService.cs
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Contracts.DTOs;

namespace Skanly.Application.Features.Contracts.Interfaces;

public interface IPdfContractService
{
    /// <summary>
    /// Generates a rental contract PDF for a confirmed booking.
    /// Saves the PDF via IFileStorageService and creates a Contract entity.
    /// Called automatically after payment confirmation.
    /// </summary>
    Task<ServiceResult<ContractDto>> GenerateForBookingAsync(
        int bookingId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the contract for a specific booking.
    /// Caller must be the student, owner, or an Admin.
    /// </summary>
    Task<ServiceResult<ContractDto>> GetByBookingAsync(
        string requesterId,
        int bookingId,
        bool isAdmin = false,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the raw PDF bytes for download.
    /// </summary>
    Task<ServiceResult<byte[]>> GetPdfBytesAsync(
        string requesterId,
        int bookingId,
        bool isAdmin = false,
        CancellationToken ct = default);

    /// <summary>
    /// Regenerates a contract PDF (Admin only).
    /// Replaces the existing PDF file and updates the Contract entity.
    /// </summary>
    Task<ServiceResult<ContractDto>> RegenerateAsync(
        int bookingId,
        CancellationToken ct = default);
}