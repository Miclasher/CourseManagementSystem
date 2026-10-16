using Application.Abstractions.Messaging;
using Application.Enrollments.Drop;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Enrollments;

internal sealed class Drop : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("enrollments/{id:guid}", async (
            Guid id,
            ICommandHandler<DropEnrollmentCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DropEnrollmentCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Enrollments);
    }
}
