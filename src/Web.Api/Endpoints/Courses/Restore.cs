using Application.Abstractions.Messaging;
using Application.Courses.Restore;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class Restore : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("courses/{id:guid}/restore", async (
            Guid id,
            ICommandHandler<RestoreCourseCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RestoreCourseCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
