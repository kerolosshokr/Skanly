// Skanly.Application/Features/Payments/DTOs/InitiatePaymentDto.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Payments.DTOs;

/// <summary>
/// Submitted by the student on the checkout page.
/// </summary>
public class InitiatePaymentDto
{
    [Required]
    public int BookingId { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; }

    // Used only for card methods — simulated, never stored raw
    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Card Holder Name")]
    public string? CardHolderName { get; set; }

    [Display(Name = "Expiry (MM/YY)")]
    public string? CardExpiry { get; set; }

    [Display(Name = "CVV")]
    public string? CardCvv { get; set; }

    // Used for mobile wallet methods
    [Display(Name = "Mobile Number")]
    public string? MobileNumber { get; set; }
}