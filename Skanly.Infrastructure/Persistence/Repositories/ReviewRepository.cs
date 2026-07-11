// Skanly.Infrastructure/Persistence/Repositories/ReviewRepository.cs
using Microsoft.EntityFrameworkCore;
using Skanly.Application.Common.Interfaces.Repositories;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Repositories;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(SkanlyDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Review>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.PropertyId == propertyId)
            .Include(r => r.Student)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Review>> GetByStudentIdAsync(
        string studentId,
        CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .Include(r => r.Property)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<Review?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(r => r.BookingId == bookingId, ct);

    public async Task<bool> HasReviewedBookingAsync(int bookingId, CancellationToken ct = default)
        => await _dbSet.AnyAsync(r => r.BookingId == bookingId, ct);

    public async Task<decimal> CalculateAverageRatingAsync(int propertyId, CancellationToken ct = default)
    {
        var avg = await _dbSet
            .Where(r => r.PropertyId == propertyId)
            .AverageAsync(r => (double?)
                ((r.CleanlinessRating + r.SafetyRating + r.InternetRating +
                  r.LocationRating + r.QuietnessRating + r.OverallRating) / 6.0), ct);

        return (decimal)Math.Round(avg ?? 0, 2);
    }
}