using FluentValidation;
using Skanly.Application.DTOs.Property;
namespace Skanly.Application.Validators.Property
{
    public  class CreatePropertyDtoValidator : AbstractValidator<CreatePropertyDto>
    {
        public CreatePropertyDtoValidator() 
        {
        }
    }
}
