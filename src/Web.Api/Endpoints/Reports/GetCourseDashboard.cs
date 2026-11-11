using Application.Abstractions.Messaging;
using Application.Reports;
using Application.Reports.CourseDashboard;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetCourseDashboard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Map(app, "reports/pg/courses/{id:guid}/dashboard", ReportSource.Postgres);
        Map(app, "reports/mongo/courses/{id:guid}/dashboard", ReportSource.Mongo);
    }

    private static void Map(IEndpointRouteBuilder app, string pattern, ReportSource source) =>
        app.MapGet(pattern, async (
            Guid id,
            IQueryHandler<GetCourseDashboardQuery, CourseDashboardResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCourseDashboardQuery(source, id);

            Result<CourseDashboardResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reports);
}
