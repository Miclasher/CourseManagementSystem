using Application.Abstractions.Messaging;
using Application.Quizzes.SubmitAttempt;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Quizzes;

internal sealed class SubmitAttempt : IEndpoint
{
    public sealed class Request
    {
        public Guid EnrollmentId { get; set; }
        public List<int> Answers { get; set; } = [];
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("quizzes/{id:guid}/attempts", async (
            Guid id,
            Request request,
            ICommandHandler<SubmitQuizAttemptCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SubmitQuizAttemptCommand(id, request.EnrollmentId, request.Answers);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Quizzes);
    }
}
