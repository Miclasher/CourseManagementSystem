using FluentValidation;

namespace Application.Enrollments.Enroll;

public class EnrollCommandValidator : AbstractValidator<EnrollCommand>
{
    public EnrollCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}
