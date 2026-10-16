using FluentValidation;

namespace Application.Enrollments.CompleteLesson;

public class CompleteLessonCommandValidator : AbstractValidator<CompleteLessonCommand>
{
    public CompleteLessonCommandValidator()
    {
        RuleFor(c => c.EnrollmentId).NotEmpty();
        RuleFor(c => c.LessonId).NotEmpty();
    }
}
