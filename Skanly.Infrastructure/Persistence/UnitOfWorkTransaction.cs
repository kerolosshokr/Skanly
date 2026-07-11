// Skanly.Infrastructure/Persistence/UnitOfWorkTransaction.cs
using Microsoft.EntityFrameworkCore.Storage;
using Skanly.Application.Common.Interfaces;

namespace Skanly.Infrastructure.Persistence;

/// <summary>
/// Wraps EF Core's IDbContextTransaction behind the Application-layer
/// IUnitOfWorkTransaction abstraction — EF Core type never leaks up.
/// </summary>
internal sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;
    private bool _disposed;

    public UnitOfWorkTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        await _transaction.CommitAsync(ct);
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        await _transaction.RollbackAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _transaction.DisposeAsync();
            _disposed = true;
        }
    }
}