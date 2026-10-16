using Application.Abstractions.Messaging;
using Application.Courses.GetStats;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class GetStats : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/stats", async (
            IQueryHandler<GetCourseStatsQuery, List<CourseStatsResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCourseStatsQuery();

            Result<List<CourseStatsResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
