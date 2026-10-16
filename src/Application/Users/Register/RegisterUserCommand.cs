using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role) : ICommand<Guid>;
