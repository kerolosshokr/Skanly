using FluentValidation;
using Skanly.Application.DTOs.Property;

namespace Skanly.Application.Validators.Property
{
    public  class UpdatePropertyDtoValidator : AbstractValidator<UpdatePropertyDto>
    {
        public UpdatePropertyDtoValidator() 
        {
        }
    }
}
