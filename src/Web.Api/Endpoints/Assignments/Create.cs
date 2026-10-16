using Application.Abstractions.Messaging;
using Application.Assignments.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Assignments;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxScore { get; set; } = 100;
        public DateTime? DueAt { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lessons/{lessonId:guid}/assignments", async (
            Guid lessonId,
            Request request,
            ICommandHandler<CreateAssignmentCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateAssignmentCommand(lessonId, request.Title, request.Description, request.MaxScore, request.DueAt);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Assignments);
    }
}
