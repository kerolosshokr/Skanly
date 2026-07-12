// Skanly.Application/Features/Owners/DTOs/OwnerPropertySummaryDto.cs
namespace Skanly.Application.Features.Owners.DTOs;

public class OwnerPropertySummaryDto
{
    public int PropertyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AreaNameEn { get; init; } = string.Empty;
    public string PropertyTypeDisplay { get; init; } = string.Empty;
    public decimal PricePerMonth { get; init; }
    public decimal AverageRating { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsApproved { get; init; }
    public bool IsDeleted { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public int TotalBookings { get; init; }
    public int ActiveBookings { get; init; }
    public DateTime CreatedAt { get; init; }

    public string StatusDisplay => IsDeleted ? "Deleted"
        : !IsApproved ? "Pending Approval"
        : IsAvailable ? "Available"
        : "Occupied";

    public string StatusBadgeClass => IsDeleted ? "bg-secondary"
        : !IsApproved ? "bg-warning text-dark"
        : IsAvailable ? "bg-success"
        : "bg-info";
}