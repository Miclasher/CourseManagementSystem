using FluentValidation;

namespace Application.Quizzes.Create;

public class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.PassPercent).InclusiveBetween(1, 100);
        RuleFor(c => c.MaxAttempts).GreaterThan(0);
        RuleFor(c => c.Questions).NotEmpty();
        RuleForEach(c => c.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.Text).NotEmpty();
            question.RuleFor(q => q.Options).Must(options => options.Count >= 2);
            question.RuleFor(q => q.Points).GreaterThan(0);
            question.RuleFor(q => q).Must(q => q.CorrectOption >= 0 && q.CorrectOption < q.Options.Count)
                .WithMessage("CorrectOption must point at one of the options.");
        });
    }
}
