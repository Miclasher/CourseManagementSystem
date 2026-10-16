using Application.Abstractions.Messaging;
using Application.Quizzes.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Quizzes;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public string Title { get; set; } = string.Empty;
        public int PassPercent { get; set; } = 60;
        public int MaxAttempts { get; set; } = 3;
        public List<QuestionDto> Questions { get; set; } = [];
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lessons/{lessonId:guid}/quizzes", async (
            Guid lessonId,
            Request request,
            ICommandHandler<CreateQuizCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateQuizCommand(lessonId, request.Title, request.PassPercent, request.MaxAttempts, request.Questions);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Quizzes);
    }
}
