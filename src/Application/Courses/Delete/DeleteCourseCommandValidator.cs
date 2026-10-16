using FluentValidation;

namespace Application.Courses.Delete;

public class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}
