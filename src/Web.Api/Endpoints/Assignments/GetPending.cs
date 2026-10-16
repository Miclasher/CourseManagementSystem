using Application.Abstractions.Messaging;
using Application.Assignments.GetPending;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Assignments;

internal sealed class GetPending : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("submissions/pending", async (
            IQueryHandler<GetPendingSubmissionsQuery, List<PendingSubmissionResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPendingSubmissionsQuery();

            Result<List<PendingSubmissionResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Assignments);
    }
}
