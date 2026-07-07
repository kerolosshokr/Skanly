using FluentValidation;
using Skanly.Application.DTOs.Payment;

namespace Skanly.Application.Validators.Payment
{
    public  class UpdatePaymentDtoValidator : AbstractValidator<UpdatePaymentDto>
    {
        public UpdatePaymentDtoValidator() 
        {
        }
    }
}
