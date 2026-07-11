// Skanly.Infrastructure/Persistence/Repositories/ReportRepository.cs
using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;
using Skanly.Domain.Enums;
using Skanly.Domain_1.Enums;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class ReportRepository : GenericRepository<Report>, IReportRepository
{
    public ReportRepository(SkanlyDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Report> Items, int TotalCount)> GetActiveReportsAsync(
        int pageNumber,
        int pageSize,
        ReportStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking();

        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Report>> GetByReporterIdAsync(
        string reporterId,
        CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.ReporterId == reporterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<Report?> GetDetailAsync(int reportId, CancellationToken ct = default)
        => await _dbSet
            .Include(r => r.ReportedProperty)
            .Include(r => r.ResolvedByAdmin)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
}