// Skanly.Application/Common/Interfaces/Repositories/IReportRepository.cs
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IReportRepository : IRepository<Report>
{
    /// <summary>Returns all open / under-investigation reports for Admin.</summary>
    Task<(IReadOnlyList<Report> Items, int TotalCount)> GetActiveReportsAsync(
        int pageNumber,
        int pageSize,
        ReportStatus? statusFilter = null,
        CancellationToken ct = default);

    /// <summary>Returns all reports submitted by a user.</summary>
    Task<IReadOnlyList<Report>> GetByReporterIdAsync(
        string reporterId,
        CancellationToken ct = default);

    Task<Report?> GetDetailAsync(int reportId, CancellationToken ct = default);
}