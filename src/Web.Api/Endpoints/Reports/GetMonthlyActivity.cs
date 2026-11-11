using Application.Abstractions.Messaging;
using Application.Reports;
using Application.Reports.MonthlyActivity;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetMonthlyActivity : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Map(app, "reports/pg/activity/monthly", ReportSource.Postgres);
        Map(app, "reports/clickhouse/activity/monthly", ReportSource.ClickHouse);
    }

    private static void Map(IEndpointRouteBuilder app, string pattern, ReportSource source) =>
        app.MapGet(pattern, async (
            IQueryHandler<GetMonthlyActivityQuery, MonthlyActivityResponse> handler,
            CancellationToken cancellationToken,
            int year = 2025) =>
        {
            var query = new GetMonthlyActivityQuery(source, year);

            Result<MonthlyActivityResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reports);
}
