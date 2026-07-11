using Skanly.Application.Common.Interfaces.Repositories;

namespace Skanly.Application.Common.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
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

    IRepository<TEntity> Repository<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);

    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken ct = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken ct = default);
}