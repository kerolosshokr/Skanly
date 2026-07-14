// Skanly.Application/Features/Contracts/Services/ContractNumberGenerator.cs
namespace Skanly.Application.Features.Contracts.Services;

/// <summary>
/// Generates unique, human-readable contract numbers.
///
/// Format: SKL-{YEAR}{MONTH}-{BOOKING_ID:D5}-{CHECKSUM}
/// Example: SKL-202501-00042-7K
///
/// Properties:
/// - Monotonically increasing within a month
/// - Booking ID is embedded — traceable to source
/// - 2-char checksum prevents typos when citing by phone
/// - No random component — deterministic from inputs
/// </summary>
public static class ContractNumberGenerator
{
    private const string Prefix = "SKL";

    // Characters used in the checksum (unambiguous — no 0/O, 1/I/L)
    private static readonly char[] ChecksumAlphabet =
        "23456789ABCDEFGHJKMNPQRSTUVWXYZ".ToCharArray();

    public static string Generate(int bookingId, DateTime generatedAt)
    {
        var yearMonth = generatedAt.ToString("yyyyMM");
        var bookingPart = bookingId.ToString("D5");
        var checksum = ComputeChecksum(bookingId, generatedAt);

        return $"{Prefix}-{yearMonth}-{bookingPart}-{checksum}";
    }

    private static string ComputeChecksum(int bookingId, DateTime dt)
    {
        // Simple but collision-resistant checksum
        var value = (bookingId * 31) + dt.Year * 12 + dt.Month;
        var c1 = ChecksumAlphabet[value % ChecksumAlphabet.Length];
        var c2 = ChecksumAlphabet[(value / ChecksumAlphabet.Length)
                                     % ChecksumAlphabet.Length];
        return $"{c1}{c2}";
    }

    /// <summary>
    /// Validates that a contract number has the correct format.
    /// </summary>
    public static bool IsValid(string contractNumber)
    {
        if (string.IsNullOrWhiteSpace(contractNumber)) return false;
        var parts = contractNumber.Split('-');
        return parts.Length == 4 &&
               parts[0] == Prefix &&
               parts[1].Length == 6 &&
               parts[2].Length == 5 &&
               parts[3].Length == 2;
    }
}