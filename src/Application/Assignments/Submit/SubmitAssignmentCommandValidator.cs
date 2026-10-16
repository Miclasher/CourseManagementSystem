using FluentValidation;

namespace Application.Assignments.Submit;

public class SubmitAssignmentCommandValidator : AbstractValidator<SubmitAssignmentCommand>
{
    public SubmitAssignmentCommandValidator()
    {
        RuleFor(c => c.AssignmentId).NotEmpty();
        RuleFor(c => c.EnrollmentId).NotEmpty();
        RuleFor(c => c.Content).NotEmpty();
    }
}
