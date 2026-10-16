using Application.Abstractions.Messaging;
using Application.Reviews.GetByCourse;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reviews;

internal sealed class GetByCourse : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/{courseId:guid}/reviews", async (
            Guid courseId,
            IQueryHandler<GetCourseReviewsQuery, List<ReviewResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCourseReviewsQuery(courseId);

            Result<List<ReviewResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reviews);
    }
}
