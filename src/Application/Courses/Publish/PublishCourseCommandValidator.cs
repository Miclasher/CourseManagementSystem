using FluentValidation;

namespace Application.Courses.Publish;

public class PublishCourseCommandValidator : AbstractValidator<PublishCourseCommand>
{
    public PublishCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}
