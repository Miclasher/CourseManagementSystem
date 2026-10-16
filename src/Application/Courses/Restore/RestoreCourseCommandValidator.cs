using FluentValidation;

namespace Application.Courses.Restore;

public class RestoreCourseCommandValidator : AbstractValidator<RestoreCourseCommand>
{
    public RestoreCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}
