namespace Skanly.Application.Features.Owners.DTOs;

public class HandleBookingRequestDto
{
    public int BookingId { get; set; }
    public bool Accept { get; set; }
    public string? RejectionReason { get; set; }
}
