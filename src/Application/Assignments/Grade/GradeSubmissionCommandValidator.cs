using FluentValidation;

namespace Application.Assignments.Grade;

public class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
{
    public GradeSubmissionCommandValidator()
    {
        RuleFor(c => c.SubmissionId).NotEmpty();
        RuleFor(c => c.Score).GreaterThanOrEqualTo(0);
    }
}
