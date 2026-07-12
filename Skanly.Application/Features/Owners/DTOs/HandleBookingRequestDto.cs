// Skanly.Application/Features/Owners/DTOs/HandleBookingRequestDto.cs
namespace Skanly.Application.Features.Owners.DTOs;

public class HandleBookingRequestDto
{
    public int BookingId { get; set; }
    public bool Accept { get; set; }          // true = Accept, false = Reject
    public string? RejectionReason { get; set; }
}