using Application.Abstractions.Messaging;
using Application.Reports;
using Application.Reports.TopCourses;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetTopCourses : IEndpoint
{
    private static readonly DateOnly DefaultFrom = new(2025, 1, 1);
    private static readonly DateOnly DefaultTo = new(2025, 12, 31);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Map(app, "reports/pg/courses/top", ReportSource.Postgres);
        Map(app, "reports/clickhouse/courses/top", ReportSource.ClickHouse);
    }

    private static void Map(IEndpointRouteBuilder app, string pattern, ReportSource source) =>
        app.MapGet(pattern, async (
            IQueryHandler<GetTopCoursesQuery, TopCoursesResponse> handler,
            CancellationToken cancellationToken,
            DateOnly? from,
            DateOnly? to,
            int limit = 20) =>
        {
            var query = new GetTopCoursesQuery(source, from ?? DefaultFrom, to ?? DefaultTo, limit);

            Result<TopCoursesResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reports);
}
