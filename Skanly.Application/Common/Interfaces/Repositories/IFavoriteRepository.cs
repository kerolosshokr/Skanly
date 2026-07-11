using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IFavoriteRepository : IRepository<Favorite>
{
    Task<bool> IsFavoritedAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Favorite>> GetByStudentIdAsync(
        string studentId,
        CancellationToken ct = default);

    Task<Favorite?> GetByStudentAndPropertyAsync(
        string studentId,
        int propertyId,
        CancellationToken ct = default);
}