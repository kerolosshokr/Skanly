// Skanly.Application/Features/Bookings/DTOs/CancelBookingDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Bookings.DTOs;

public class CancelBookingDto
{
    [Required]
    public int BookingId { get; set; }

    [Required]
    [Display(Name = "Cancellation Reason")]
    public string Reason { get; set; } = string.Empty;
}