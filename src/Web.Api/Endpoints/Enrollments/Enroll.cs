using Application.Abstractions.Messaging;
using Application.Enrollments.Enroll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Enrollments;

internal sealed class Enroll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("courses/{courseId:guid}/enrollments", async (
            Guid courseId,
            ICommandHandler<EnrollCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new EnrollCommand(courseId);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Enrollments);
    }
}
