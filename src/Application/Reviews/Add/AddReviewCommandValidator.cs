using FluentValidation;

namespace Application.Reviews.Add;

public class AddReviewCommandValidator : AbstractValidator<AddReviewCommand>
{
    public AddReviewCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.Rating).InclusiveBetween((short)1, (short)5);
        RuleFor(c => c.Comment).MaximumLength(2000);
    }
}
