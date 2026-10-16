using Application.Abstractions.Messaging;
using Application.Enrollments.GetMine;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Enrollments;

internal sealed class GetMine : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("enrollments/mine", async (
            IQueryHandler<GetMyEnrollmentsQuery, List<EnrollmentResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetMyEnrollmentsQuery();

            Result<List<EnrollmentResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Enrollments);
    }
}
