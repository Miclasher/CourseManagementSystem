using Application.Abstractions.Messaging;
using Application.Courses.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/{id:guid}", async (
            Guid id,
            IQueryHandler<GetCourseByIdQuery, CourseResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCourseByIdQuery(id);

            Result<CourseResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
