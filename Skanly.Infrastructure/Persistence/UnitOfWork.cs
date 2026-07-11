using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Infrastructure.Persistence.Repositories;

namespace Skanly.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SkanlyDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

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

    private readonly Dictionary<Type, object> _genericRepositories = new();

    private bool _disposed;

    public UnitOfWork(SkanlyDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

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

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        _logger.LogDebug("Database transaction started. Id: {TransactionId}", transaction.TransactionId);
        return new UnitOfWorkTransaction(transaction);
    }

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

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _context.DisposeAsync();
            _disposed = true;
        }
    }
}