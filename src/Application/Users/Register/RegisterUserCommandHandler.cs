using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Users;
using SharedKernel;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(IUserRepository users, IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var user = User.Register(
            command.Email,
            passwordHasher.Hash(command.Password),
            command.FirstName,
            command.LastName,
            command.Role);

        users.Add(user);

        return Task.FromResult<Result<Guid>>(user.Id);
    }
}
