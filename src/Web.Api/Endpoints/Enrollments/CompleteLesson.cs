using Application.Abstractions.Messaging;
using Application.Enrollments.CompleteLesson;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Enrollments;

internal sealed class CompleteLesson : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("enrollments/{id:guid}/lessons/{lessonId:guid}/complete", async (
            Guid id,
            Guid lessonId,
            ICommandHandler<CompleteLessonCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CompleteLessonCommand(id, lessonId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Enrollments);
    }
}
