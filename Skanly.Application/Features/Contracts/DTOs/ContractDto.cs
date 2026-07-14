// Skanly.Application/Features/Contracts/DTOs/ContractDto.cs
namespace Skanly.Application.Features.Contracts.DTOs;

public class ContractDto
{
    public int ContractId { get; init; }
    public int BookingId { get; init; }
    public string ContractNumber { get; init; } = string.Empty;
    public string PdfUrl { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public string TimeAgo => GetTimeAgo(GeneratedAt);

    // Parties
    public string StudentFullName { get; init; } = string.Empty;
    public string StudentNationalId { get; init; } = string.Empty;
    public string? StudentPhone { get; init; }
    public string OwnerFullName { get; init; } = string.Empty;
    public string OwnerNationalId { get; init; } = string.Empty;
    public string? OwnerPhone { get; init; }

    // Property
    public string PropertyTitle { get; init; } = string.Empty;
    public string PropertyAddress { get; init; } = string.Empty;
    public string PropertyTypeDisplay { get; init; } = string.Empty;

    // Financial
    public decimal MonthlyRent { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal CommissionRate { get; init; }

    // Dates
    public DateOnly CheckInDate { get; init; }
    public DateOnly? CheckOutDate { get; init; }

    private static string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        return span.TotalMinutes < 60
            ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24
                ? $"{(int)span.TotalHours}h ago"
                : dt.ToString("MMM dd, yyyy");
    }
}