// Skanly.Application/Common/Interfaces/IUnitOfWorkTransaction.cs
namespace Skanly.Application.Common.Interfaces;

/// <summary>
/// Abstraction over a database transaction so Application layer
/// can commit / rollback without depending on EF Core types.
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}