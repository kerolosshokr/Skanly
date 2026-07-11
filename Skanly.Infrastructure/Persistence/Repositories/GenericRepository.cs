// Skanly.Infrastructure/Persistence/Repositories/GenericRepository.cs
using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces;
using Skanly.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace Skanly.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IRepository<T>.
/// All specific repositories inherit from this — they get full
/// CRUD for free and only add their entity-specific query methods.
/// </summary>
public class GenericRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    protected readonly SkanlyDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(SkanlyDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // ── Single Entity Queries ────────────────────────────────────────────────

    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;
        query = ApplyIncludes(query, includes);
        return await query.FirstOrDefaultAsync(predicate, ct);
    }

    // ── Collection Queries ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate is not null)
            query = query.Where(predicate);

        query = ApplyIncludes(query, includes);

        if (orderBy is not null)
            query = orderBy(query);

        return await query.ToListAsync(ct);
    }

    // ── Paged Queries ─────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate is not null)
            query = query.Where(predicate);

        query = ApplyIncludes(query, includes);

        int totalCount = await query.CountAsync(ct);

        if (orderBy is not null)
            query = orderBy(query);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    // ── Existence / Count ─────────────────────────────────────────────────────

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
        => predicate is null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    // ── Raw IQueryable ─────────────────────────────────────────────────────────

    public IQueryable<TEntity> Query()
        => _dbSet.AsQueryable();

    public IQueryable<TEntity> QueryNoTracking()
        => _dbSet.AsNoTracking();

    // ── Write Operations ──────────────────────────────────────────────────────

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(entities, ct);

    public void Update(TEntity entity)
        => _dbSet.Update(entity);

    public void UpdateRange(IEnumerable<TEntity> entities)
        => _dbSet.UpdateRange(entities);

    public void Remove(TEntity entity)
        => _dbSet.Remove(entity);

    public void RemoveRange(IEnumerable<TEntity> entities)
        => _dbSet.RemoveRange(entities);

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static IQueryable<TEntity> ApplyIncludes(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, object>>[] includes)
    {
        return includes.Aggregate(query,
            (current, include) => current.Include(include));
    }
}