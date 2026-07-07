using FluentValidation;
using Skanly.Application.DTOs.Payment;

namespace Skanly.Application.Validators.Payment
{
    public  class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentDtoValidator() 
        {
        }
    }
}
