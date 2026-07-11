// Skanly.Application/Common/Interfaces/Repositories/IOwnerRepository.cs
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IOwnerRepository : IRepository<Owner>
{
    Task<Owner?> GetByUserIdAsync(string userId, CancellationToken ct = default);

    Task<Owner?> GetWithPropertiesAsync(string userId, CancellationToken ct = default);

    /// <summary>Returns total earnings for an owner (sum of confirmed booking totals minus commission).</summary>
    Task<decimal> GetTotalEarningsAsync(string ownerId, CancellationToken ct = default);

    Task<int> GetTotalCountAsync(CancellationToken ct = default);
}