using FluentValidation;
using Skanly.Application.DTOs.University;

namespace Skanly.Application.Validators.University
{
    public  class CreateUniversityDtoValidator : AbstractValidator<CreateUniversityDto>
    {
        public CreateUniversityDtoValidator() 
        {
        }
    }
}
