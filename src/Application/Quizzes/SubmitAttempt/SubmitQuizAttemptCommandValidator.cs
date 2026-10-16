using FluentValidation;

namespace Application.Quizzes.SubmitAttempt;

public class SubmitQuizAttemptCommandValidator : AbstractValidator<SubmitQuizAttemptCommand>
{
    public SubmitQuizAttemptCommandValidator()
    {
        RuleFor(c => c.QuizId).NotEmpty();
        RuleFor(c => c.EnrollmentId).NotEmpty();
        RuleFor(c => c.Answers).NotEmpty();
    }
}
