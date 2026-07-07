using FluentValidation;
using Skanly.Application.DTOs.Booking;

namespace Skanly.Application.Validators.Booking
{
    public  class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
    {
        public CreateBookingDtoValidator()
        {
        }
    }
}
