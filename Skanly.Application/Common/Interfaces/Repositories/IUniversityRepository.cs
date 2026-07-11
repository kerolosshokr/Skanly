// Skanly.Application/Common/Interfaces/Repositories/IUniversityRepository.cs
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IUniversityRepository : IRepository<University>
{
    Task<IReadOnlyList<University>> GetActiveAsync(CancellationToken ct = default);

    Task<University?> GetByNameEnAsync(string nameEn, CancellationToken ct = default);

    /// <summary>Returns universities ordered by number of properties (for analytics).</summary>
    Task<IReadOnlyList<(University University, int PropertyCount)>> GetMostPopularAsync(
        int top,
        CancellationToken ct = default);
}