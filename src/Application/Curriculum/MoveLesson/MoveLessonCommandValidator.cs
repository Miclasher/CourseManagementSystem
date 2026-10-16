using FluentValidation;

namespace Application.Curriculum.MoveLesson;

public class MoveLessonCommandValidator : AbstractValidator<MoveLessonCommand>
{
    public MoveLessonCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.Position).GreaterThan(0);
    }
}
