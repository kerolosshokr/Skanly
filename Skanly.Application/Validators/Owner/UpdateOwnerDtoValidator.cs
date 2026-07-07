using FluentValidation;
using Skanly.Application.DTOs.Owner;

namespace Skanly.Application.Validators.Owner
{
    public  class UpdateOwnerDtoValidator : AbstractValidator<UpdateOwnerDto>
    {
        public UpdateOwnerDtoValidator() 
        {
        }
    }
}
