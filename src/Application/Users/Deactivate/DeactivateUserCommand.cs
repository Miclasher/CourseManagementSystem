using Application.Abstractions.Messaging;

namespace Application.Users.Deactivate;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
