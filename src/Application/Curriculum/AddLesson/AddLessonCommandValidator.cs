using FluentValidation;

namespace Application.Curriculum.AddLesson;

public class AddLessonCommandValidator : AbstractValidator<AddLessonCommand>
{
    public AddLessonCommandValidator()
    {
        RuleFor(c => c.ModuleId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.DurationMinutes).GreaterThanOrEqualTo(0);
    }
}
