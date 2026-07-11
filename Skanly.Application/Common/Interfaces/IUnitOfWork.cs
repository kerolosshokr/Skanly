// Skanly.Application/Common/Interfaces/IUnitOfWork.cs
using Skanly.Application.Common.Interfaces.Repositories;

namespace Skanly.Application.Common.Interfaces;

/// <summary>
/// Unit of Work interface — the single entry point that Application
/// services use to access ALL repositories and commit changes.
///
/// Rules for Application layer code:
/// 1. Always access repositories through IUnitOfWork, never inject
///    individual repositories directly into services.
/// 2. Call SaveChangesAsync() exactly once at the end of each
///    business operation (not inside loops, not inside repositories).
/// 3. Use BeginTransactionAsync() only when you need explicit
///    cross-operation rollback control (e.g. booking + payment + contract
///    must all succeed or all fail together).
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    // ── Repositories ──────────────────────────────────────────────────────────

    IPropertyRepository Properties { get; }
    IBookingRepository Bookings { get; }
    IStudentRepository Students { get; }
    IOwnerRepository Owners { get; }
    IChatRepository Chat { get; }
    IReviewRepository Reviews { get; }
    IReportRepository Reports { get; }
    INotificationRepository Notifications { get; }
    IFavoriteRepository Favorites { get; }
    IUniversityRepository Universities { get; }

    /// <summary>
    /// Generic repository accessor for entities that don't need
    /// a dedicated specific repository (e.g. Amenity, Area, Contract,
    /// Payment, CommissionSetting, IdentityVerification).
    /// </summary>
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;

    // ── Persistence ───────────────────────────────────────────────────────────

    /// <summary>
    /// Persists all tracked changes to the database.
    /// Returns the number of state entries written.
    /// Call this exactly once per business operation.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    // ── Transaction Control ───────────────────────────────────────────────────

    /// <summary>
    /// Begins an explicit database transaction.
    /// Use for multi-step operations where partial failure
    /// must leave the database unchanged.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Executes an operation inside an implicit transaction with automatic retry.</summary>
    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken ct = default);

    /// <summary>Executes an operation inside an implicit transaction and returns a result.</summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken ct = default);
}