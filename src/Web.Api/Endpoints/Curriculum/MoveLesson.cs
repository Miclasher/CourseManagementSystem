using Application.Abstractions.Messaging;
using Application.Curriculum.MoveLesson;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Curriculum;

internal sealed class MoveLesson : IEndpoint
{
    public sealed class Request
    {
        public int Position { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("lessons/{lessonId:guid}/position", async (
            Guid lessonId,
            Request request,
            ICommandHandler<MoveLessonCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new MoveLessonCommand(lessonId, request.Position);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Curriculum);
    }
}
