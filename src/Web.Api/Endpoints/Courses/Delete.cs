using Application.Abstractions.Messaging;
using Application.Courses.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("courses/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteCourseCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteCourseCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
