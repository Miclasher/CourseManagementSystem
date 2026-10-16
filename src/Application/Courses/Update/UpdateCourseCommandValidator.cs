using FluentValidation;

namespace Application.Courses.Update;

public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Level).IsInEnum();
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.MaxStudents).GreaterThan(0).When(c => c.MaxStudents.HasValue);
    }
}
