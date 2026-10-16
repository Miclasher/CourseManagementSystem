using Application.Abstractions.Messaging;
using Application.Assignments.Grade;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Assignments;

internal sealed class Grade : IEndpoint
{
    public sealed class Request
    {
        public int Score { get; set; }
        public string? Feedback { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("submissions/{id:guid}/grade", async (
            Guid id,
            Request request,
            ICommandHandler<GradeSubmissionCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GradeSubmissionCommand(id, request.Score, request.Feedback);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Assignments);
    }
}
