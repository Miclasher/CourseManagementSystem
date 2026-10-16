using Application.Abstractions.Messaging;
using Application.Courses.GetCatalog;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class GetCatalog : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses", async (
            IQueryHandler<GetCourseCatalogQuery, List<CatalogItemResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<List<CatalogItemResponse>> result = await handler.Handle(new GetCourseCatalogQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
