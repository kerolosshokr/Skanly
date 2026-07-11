using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    Task<IReadOnlyList<Review>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Review>> GetByStudentIdAsync(
        string studentId,
        CancellationToken ct = default);

    Task<Review?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default);

    Task<bool> HasReviewedBookingAsync(int bookingId, CancellationToken ct = default);

    Task<decimal> CalculateAverageRatingAsync(int propertyId, CancellationToken ct = default);
}
