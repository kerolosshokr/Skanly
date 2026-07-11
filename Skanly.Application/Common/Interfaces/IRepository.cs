// Skanly.Application/Common/Interfaces/IRepository.cs
using System.Linq.Expressions;

namespace Skanly.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface providing standard CRUD and query operations
/// for all aggregate root entities. Services depend only on this abstraction —
/// never on EF Core directly.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    // ── Single Entity Queries ────────────────────────────────────────────────

    /// <summary>Gets an entity by its primary key.</summary>
    Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Gets first entity matching a predicate, or null.</summary>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// Gets first entity matching a predicate with optional eager loading.
    /// includes: navigation properties to Include (e.g. p => p.Images)
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    // ── Collection Queries ────────────────────────────────────────────────────

    /// <summary>Returns all entities (use with caution on large tables).</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all entities matching a predicate.</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>Returns entities with eager loading and optional ordering.</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    // ── Paged Queries ─────────────────────────────────────────────────────────

    /// <summary>Returns a paged result set.</summary>
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    // ── Existence / Count ─────────────────────────────────────────────────────

    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    // ── Raw IQueryable (for complex queries in specific repos) ─────────────────

    /// <summary>
    /// Returns an IQueryable for building complex queries.
    /// Caller must not Save — only used for reads.
    /// </summary>
    IQueryable<TEntity> Query();

    IQueryable<TEntity> QueryNoTracking();

    // ── Write Operations ──────────────────────────────────────────────────────

    Task AddAsync(TEntity entity, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    void Update(TEntity entity);

    void UpdateRange(IEnumerable<TEntity> entities);

    void Remove(TEntity entity);

    void RemoveRange(IEnumerable<TEntity> entities);
}