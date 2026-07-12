// Skanly.Application/Features/Payments/DTOs/PaymentSummaryDto.cs
namespace Skanly.Application.Features.Payments.DTOs;

public class PaymentSummaryDto
{
    public decimal TotalCollected { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalOwnerPayouts { get; init; }
    public int TotalTransactions { get; init; }
    public int SuccessfulTransactions { get; init; }
    public int FailedTransactions { get; init; }
    public decimal SuccessRate =>
        TotalTransactions == 0
            ? 0
            : Math.Round((decimal)SuccessfulTransactions / TotalTransactions * 100, 1);

    public IReadOnlyList<MethodBreakdown> ByMethod { get; init; }
        = new List<MethodBreakdown>();
}

public class MethodBreakdown
{
    public string Method { get; init; } = string.Empty;
    public string MethodIcon { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal TotalAmount { get; init; }
}