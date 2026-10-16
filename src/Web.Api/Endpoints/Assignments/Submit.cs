using Application.Abstractions.Messaging;
using Application.Assignments.Submit;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Assignments;

internal sealed class Submit : IEndpoint
{
    public sealed class Request
    {
        public Guid EnrollmentId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("assignments/{id:guid}/submissions", async (
            Guid id,
            Request request,
            ICommandHandler<SubmitAssignmentCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SubmitAssignmentCommand(id, request.EnrollmentId, request.Content);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Assignments);
    }
}
