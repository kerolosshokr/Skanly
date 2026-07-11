using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class FavoriteRepository : GenericRepository<Favorite>, IFavoriteRepository
{
    public FavoriteRepository(SkanlyDbContext context) : base(context) { }

    public async Task<bool> IsFavoritedAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default)
        => await _dbSet.AnyAsync(
            f => f.StudentId == studentId && f.PropertyId == propertyId, ct);

    public async Task<IReadOnlyList<Favorite>> GetByStudentIdAsync(
        string studentId,
        CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(f => f.StudentId == studentId)
            .Include(f => f.Property).ThenInclude(p => p.Area)
            .Include(f => f.Property).ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

    public async Task<Favorite?> GetByStudentAndPropertyAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(
            f => f.StudentId == studentId && f.PropertyId == propertyId, ct);
}