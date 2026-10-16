using Application.Abstractions.Messaging;
using Application.Courses.Publish;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class Publish : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("courses/{id:guid}/publish", async (
            Guid id,
            ICommandHandler<PublishCourseCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new PublishCourseCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
