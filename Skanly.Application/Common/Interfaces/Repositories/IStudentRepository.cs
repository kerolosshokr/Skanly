using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByUserIdAsync(string userId, CancellationToken ct = default);

    Task<Student?> GetWithUniversityAsync(string userId, CancellationToken ct = default);

    Task<Student?> GetFullProfileAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<Student>> GetPendingVerificationAsync(CancellationToken ct = default);

    Task<int> GetTotalCountAsync(CancellationToken ct = default);
}