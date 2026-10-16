using Application.Abstractions.Messaging;
using Application.Curriculum.AddModule;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Curriculum;

internal sealed class AddModule : IEndpoint
{
    public sealed class Request
    {
        public string Title { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("courses/{courseId:guid}/modules", async (
            Guid courseId,
            Request request,
            ICommandHandler<AddModuleCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddModuleCommand(courseId, request.Title);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Curriculum);
    }
}
