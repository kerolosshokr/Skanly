using FluentValidation;
using Skanly.Application.DTOs.University;
namespace Skanly.Application.Validators.University
{
    public  class UpdateUniversityDtoValidator : AbstractValidator<UpdateUniversityDto>
    {
        public UpdateUniversityDtoValidator() 
        {
        }
    }
}
