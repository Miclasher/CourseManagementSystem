using Application.Abstractions.Authentication;
using Application.Users.Deactivate;
using Domain.Users;

namespace Application.UnitTests.Users;

public sealed class DeactivateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenActorIsNeitherSelfNorAdmin()
    {
        // Arrange
        var actor = new User { Id = Guid.NewGuid(), Roles = ["student"] };
        var target = new User { Id = Guid.NewGuid(), Roles = ["student"] };
        _userContext.UserId.Returns(actor.Id);
        _users.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _users.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        var handler = new DeactivateUserCommandHandler(_users, _userContext);

        // Act
        Result result = await handler.Handle(new DeactivateUserCommand(target.Id), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(UserErrors.Unauthorized());
        _users.DidNotReceive().Deactivate(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_Should_Deactivate_WhenActorIsAdmin()
    {
        // Arrange
        var admin = new User { Id = Guid.NewGuid(), Roles = ["admin"] };
        var target = new User { Id = Guid.NewGuid(), Roles = ["student"] };
        _userContext.UserId.Returns(admin.Id);
        _users.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _users.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
        var handler = new DeactivateUserCommandHandler(_users, _userContext);

        // Act
        Result result = await handler.Handle(new DeactivateUserCommand(target.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _users.Received(1).Deactivate(target);
        target.DomainEvents.ShouldContain(e => e is UserDeactivatedDomainEvent);
    }
}
