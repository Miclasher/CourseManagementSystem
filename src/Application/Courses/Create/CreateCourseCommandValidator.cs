using FluentValidation;

namespace Application.Courses.Create;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(220).Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
        RuleFor(c => c.Level).IsInEnum();
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.MaxStudents).GreaterThan(0).When(c => c.MaxStudents.HasValue);
    }
}
