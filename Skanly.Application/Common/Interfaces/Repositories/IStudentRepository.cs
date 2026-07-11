// Skanly.Application/Common/Interfaces/Repositories/IStudentRepository.cs
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByUserIdAsync(string userId, CancellationToken ct = default);

    Task<Student?> GetWithUniversityAsync(string userId, CancellationToken ct = default);

    Task<Student?> GetFullProfileAsync(string userId, CancellationToken ct = default);

    /// <summary>Returns all students pending identity verification.</summary>
    Task<IReadOnlyList<Student>> GetPendingVerificationAsync(CancellationToken ct = default);

    Task<int> GetTotalCountAsync(CancellationToken ct = default);
}