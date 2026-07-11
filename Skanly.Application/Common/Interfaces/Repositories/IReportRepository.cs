using Skanly.Domain.Entities;
using Skanly.Domain.Enums;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IReportRepository : IRepository<Report>
{
    Task<(IReadOnlyList<Report> Items, int TotalCount)> GetActiveReportsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<Report>> GetByReporterIdAsync(
        string reporterId,
        CancellationToken ct = default);

    Task<Report?> GetDetailAsync(int reportId, CancellationToken ct = default);
}