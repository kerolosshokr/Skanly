using FluentValidation;
using Skanly.Application.DTOs.Review;

namespace Skanly.Application.Validators.Review
{
    public  class UpdateReviewDtoValidator : AbstractValidator<UpdateReviewDto>
    {
        public UpdateReviewDtoValidator() 
        {
        }
    }
}
