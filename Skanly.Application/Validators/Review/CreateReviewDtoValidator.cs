using FluentValidation;
using Skanly.Application.DTOs.Review;

namespace Skanly.Application.Validators.Review
{
    public  class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
    {
        public CreateReviewDtoValidator() 
        {
        }
    }
}
