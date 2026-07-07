using FluentValidation;
using Skanly.Application.DTOs.Booking;

namespace Skanly.Application.Validators.Booking
{
    public class UpdateBookingDtoValidator : AbstractValidator<UpdateBookingDto>
    {
        public UpdateBookingDtoValidator() 
        {
        }
    }
}
