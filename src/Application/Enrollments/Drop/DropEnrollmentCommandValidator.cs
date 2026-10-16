using FluentValidation;

namespace Application.Enrollments.Drop;

public class DropEnrollmentCommandValidator : AbstractValidator<DropEnrollmentCommand>
{
    public DropEnrollmentCommandValidator()
    {
        RuleFor(c => c.EnrollmentId).NotEmpty();
    }
}
