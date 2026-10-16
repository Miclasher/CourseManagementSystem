using Application.Abstractions.Messaging;
using Application.Reviews.Add;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reviews;

internal sealed class Add : IEndpoint
{
    public sealed class Request
    {
        public short Rating { get; set; }
        public string? Comment { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("courses/{courseId:guid}/reviews", async (
            Guid courseId,
            Request request,
            ICommandHandler<AddReviewCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddReviewCommand(courseId, request.Rating, request.Comment);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reviews);
    }
}
