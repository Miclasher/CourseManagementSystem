using FluentValidation;

namespace Application.Users.Deactivate;

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}
