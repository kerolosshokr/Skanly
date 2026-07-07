using FluentValidation;
using Skanly.Application.DTOs.Owner;

namespace Skanly.Application.Validators.Owner
{
    public  class CreateOwnerDtoValidator : AbstractValidator<CreateOwnerDto>
    {
        public CreateOwnerDtoValidator() 
        {
        }
    }
}
