using FluentValidation;
using Skanly.Application.DTOs.Student;

namespace Skanly.Application.Validators.Student
{
    public  class CreateStudentDtoVaildator : AbstractValidator<CreateStudentDto>
    {
        public CreateStudentDtoVaildator() 
        {
        }
    }
}
