// Skanly.Application/Features/Bookings/DTOs/CreateBookingDto.cs
using System.ComponentModel.DataAnnotations;

namespace Skanly.Application.Features.Bookings.DTOs;

public class CreateBookingDto
{
    [Required]
    public int PropertyId { get; set; }

    [Required]
    [Display(Name = "Check-in Date")]
    public DateOnly CheckInDate { get; set; }

    [Display(Name = "Check-out Date")]
    public DateOnly? CheckOutDate { get; set; }

    [Display(Name = "Special Requests")]
    public string? SpecialRequests { get; set; }
}