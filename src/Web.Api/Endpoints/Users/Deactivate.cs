using Application.Abstractions.Messaging;
using Application.Users.Deactivate;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Deactivate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{id:guid}", async (
            Guid id,
            ICommandHandler<DeactivateUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeactivateUserCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users);
    }
}
