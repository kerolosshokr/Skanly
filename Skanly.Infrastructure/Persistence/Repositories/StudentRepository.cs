using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class StudentRepository : GenericRepository<Student>, IStudentRepository
{
    public StudentRepository(SkanlyDbContext context) : base(context) { }

    public async Task<Student?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<Student?> GetWithUniversityAsync(string userId, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.University)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<Student?> GetFullProfileAsync(string userId, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.University)
            .Include(s => s.Bookings)
            .Include(s => s.Favorites).ThenInclude(f => f.Property)
            .Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<IReadOnlyList<Student>> GetPendingVerificationAsync(CancellationToken ct = default)
        => await _dbSet
            .Where(s => !s.IsIdentityVerified)
            .ToListAsync(ct);

    public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
        => await _dbSet.CountAsync(ct);
}