using FluentValidation;
using Skanly.Application.DTOs.Student;

namespace Skanly.Application.Validators.Student;

public class UpdateStudentDtoValidator : AbstractValidator<UpdateStudentDto>
{
    public UpdateStudentDtoValidator()
    {
    }
}