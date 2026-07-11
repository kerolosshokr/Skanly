// Skanly.Infrastructure/Persistence/Repositories/UniversityRepository.cs
using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class UniversityRepository : GenericRepository<University>, IUniversityRepository
{
    public UniversityRepository(SkanlyDbContext context) : base(context) { }

    public async Task<IReadOnlyList<University>> GetActiveAsync(CancellationToken ct = default)
        => await _dbSet
            .Where(u => u.IsActive)
            .OrderBy(u => u.NameEn)
            .ToListAsync(ct);

    public async Task<University?> GetByNameEnAsync(string nameEn, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(
            u => u.NameEn.ToLower() == nameEn.ToLower(), ct);

    public async Task<IReadOnlyList<(University University, int PropertyCount)>> GetMostPopularAsync(
        int top,
        CancellationToken ct = default)
    {
        var results = await _dbSet
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => new
            {
                University = u,
                PropertyCount = u.Properties.Count(p => p.IsApproved && !p.IsDeleted)
            })
            .OrderByDescending(x => x.PropertyCount)
            .Take(top)
            .ToListAsync(ct);

        return results
            .Select(x => (x.University, x.PropertyCount))
            .ToList();
    }
}