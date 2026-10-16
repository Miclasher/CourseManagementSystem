using Application.Abstractions.Messaging;
using Application.Curriculum.AddLesson;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Curriculum;

internal sealed class AddLesson : IEndpoint
{
    public sealed class Request
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("modules/{moduleId:guid}/lessons", async (
            Guid moduleId,
            Request request,
            ICommandHandler<AddLessonCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddLessonCommand(moduleId, request.Title, request.Content, request.DurationMinutes);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Curriculum);
    }
}
