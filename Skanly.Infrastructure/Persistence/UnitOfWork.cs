// Skanly.Infrastructure/Persistence/UnitOfWork.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Infrastructure.Persistence.Repositories;

namespace Skanly.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IUnitOfWork.
///
/// Design decisions:
/// - All specific repositories are lazily instantiated (created only when
///   first accessed). This avoids spinning up 10 repository objects for an
///   operation that only touches 1 or 2 of them.
/// - All repositories share the SAME SkanlyDbContext instance (injected via
///   DI as Scoped). This is what makes UoW work — one context = one transaction
///   boundary for an entire HTTP request.
/// - The generic Repository<T>() accessor is backed by a Dictionary cache
///   so the same TEntity always returns the same GenericRepository instance.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SkanlyDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    // Lazy-initialised specific repositories
    private IPropertyRepository? _properties;
    private IBookingRepository? _bookings;
    private IStudentRepository? _students;
    private IOwnerRepository? _owners;
    private IChatRepository? _chat;
    private IReviewRepository? _reviews;
    private IReportRepository? _reports;
    private INotificationRepository? _notifications;
    private IFavoriteRepository? _favorites;
    private IUniversityRepository? _universities;

    // Cache for generic repositories
    private readonly Dictionary<Type, object> _genericRepositories = new();

    private bool _disposed;

    public UnitOfWork(SkanlyDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Repository Accessors (lazy init) ──────────────────────────────────────

    public IPropertyRepository Properties
        => _properties ??= new PropertyRepository(_context);

    public IBookingRepository Bookings
        => _bookings ??= new BookingRepository(_context);

    public IStudentRepository Students
        => _students ??= new StudentRepository(_context);

    public IOwnerRepository Owners
        => _owners ??= new OwnerRepository(_context);

    public IChatRepository Chat
        => _chat ??= new ChatRepository(_context);

    public IReviewRepository Reviews
        => _reviews ??= new ReviewRepository(_context);

    public IReportRepository Reports
        => _reports ??= new ReportRepository(_context);

    public INotificationRepository Notifications
        => _notifications ??= new NotificationRepository(_context);

    public IFavoriteRepository Favorites
        => _favorites ??= new FavoriteRepository(_context);

    public IUniversityRepository Universities
        => _universities ??= new UniversityRepository(_context);

    /// <summary>
    /// Returns a cached GenericRepository for any entity type not
    /// covered by a specific repository (e.g. Amenity, Area, Contract).
    /// </summary>
    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);

        if (!_genericRepositories.TryGetValue(type, out var repo))
        {
            repo = new GenericRepository<TEntity>(_context);
            _genericRepositories[type] = repo;
        }

        return (IRepository<TEntity>)repo;
    }

    // ── SaveChanges ───────────────────────────────────────────────────────────

    /// <summary>
    /// Persists all tracked EF Core changes to the database.
    ///
    /// Wraps SaveChangesAsync in structured error handling:
    /// - DbUpdateConcurrencyException → logs and re-throws (caller decides retry)
    /// - DbUpdateException → logs detailed SQL error and re-throws
    /// - Any other exception → logs and re-throws
    ///
    /// The audit timestamp interceptor in SkanlyDbContext (Part 4)
    /// runs automatically before the actual SQL is sent.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex,
                "Concurrency conflict detected while saving changes. " +
                "Entities: {Entities}",
                string.Join(", ", ex.Entries.Select(e => e.Entity.GetType().Name)));
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Database update failed. Inner: {Inner}",
                ex.InnerException?.Message ?? ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SaveChangesAsync.");
            throw;
        }
    }

    // ── Transaction Control ───────────────────────────────────────────────────

    /// <summary>
    /// Opens an explicit EF Core transaction.
    ///
    /// Usage pattern in Application services:
    ///
    ///   await using var tx = await _uow.BeginTransactionAsync();
    ///   try
    ///   {
    ///       // multiple repository operations...
    ///       await _uow.SaveChangesAsync();
    ///       await tx.CommitAsync();
    ///   }
    ///   catch
    ///   {
    ///       await tx.RollbackAsync();
    ///       throw;
    ///   }
    /// </summary>
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        _logger.LogDebug("Database transaction started. Id: {TransactionId}", transaction.TransactionId);
        return new UnitOfWorkTransaction(transaction);
    }

    /// <summary>
    /// Executes an operation inside an implicit transaction.
    /// On success: commits. On any exception: rolls back then re-throws.
    /// Suitable for operations that don't need caller-side rollback control.
    /// </summary>
    public async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                await operation();
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogDebug(
                    "Transaction committed. Id: {TransactionId}",
                    transaction.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Transaction rolled back. Id: {TransactionId}",
                    transaction.TransactionId);

                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    /// <summary>
    /// Executes an operation inside an implicit transaction and returns a result.
    /// Same commit/rollback semantics as the void overload.
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogDebug(
                    "Transaction committed. Id: {TransactionId}",
                    transaction.TransactionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Transaction rolled back. Id: {TransactionId}",
                    transaction.TransactionId);

                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _context.DisposeAsync();
            _disposed = true;
        }
    }
}