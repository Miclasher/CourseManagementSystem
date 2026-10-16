using FluentValidation;

namespace Application.Curriculum.AddModule;

public class AddModuleCommandValidator : AbstractValidator<AddModuleCommand>
{
    public AddModuleCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
    }
}
