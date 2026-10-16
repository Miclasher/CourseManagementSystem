using Application.Abstractions.Messaging;
using Application.Courses.Search;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class Search : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/search", async (
            string? text,
            Guid? categoryId,
            int? page,
            int? pageSize,
            IQueryHandler<SearchCoursesQuery, List<CourseSearchResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchCoursesQuery(
                text,
                categoryId,
                Math.Max(page ?? 1, 1),
                Math.Clamp(pageSize ?? 20, 1, 100));

            Result<List<CourseSearchResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
