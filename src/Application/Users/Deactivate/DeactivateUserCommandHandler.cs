using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Users;
using SharedKernel;

namespace Application.Users.Deactivate;

internal sealed class DeactivateUserCommandHandler(IUserRepository users, IUserContext userContext)
    : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        User? target = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (target is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        User? actor = target.Id == userContext.UserId
            ? target
            : await users.GetByIdAsync(userContext.UserId, cancellationToken);

        if (actor is null || actor.Id != target.Id && !actor.IsAdmin)
        {
            return Result.Failure(UserErrors.Unauthorized());
        }

        target.Deactivate();
        users.Deactivate(target);

        return Result.Success();
    }
}
