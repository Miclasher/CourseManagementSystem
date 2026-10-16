using Application.Abstractions.Messaging;
using Application.Courses.GetDeleted;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class GetDeleted : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/deleted", async (
            IQueryHandler<GetDeletedCoursesQuery, List<DeletedCourseResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<List<DeletedCourseResponse>> result = await handler.Handle(new GetDeletedCoursesQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
