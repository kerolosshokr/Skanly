// Skanly.Application/Common/Interfaces/Repositories/IReviewRepository.cs
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Interfaces.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    /// <summary>Returns all reviews for a property, with student info.</summary>
    Task<IReadOnlyList<Review>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken ct = default);

    /// <summary>Returns all reviews written by a student.</summary>
    Task<IReadOnlyList<Review>> GetByStudentIdAsync(
        string studentId,
        CancellationToken ct = default);

    /// <summary>Returns the review for a specific booking (null if not yet reviewed).</summary>
    Task<Review?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default);

    /// <summary>Checks whether a student has already reviewed a booking.</summary>
    Task<bool> HasReviewedBookingAsync(int bookingId, CancellationToken ct = default);

    /// <summary>Calculates the new average rating for a property across all 6 categories.</summary>
    Task<decimal> CalculateAverageRatingAsync(int propertyId, CancellationToken ct = default);
}