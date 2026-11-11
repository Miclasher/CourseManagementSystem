using Application.Abstractions.Messaging;
using Application.Reports;
using Application.Reports.StudentTranscript;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetStudentTranscript : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Map(app, "reports/pg/students/{id:guid}/transcript", ReportSource.Postgres);
        Map(app, "reports/mongo/students/{id:guid}/transcript", ReportSource.Mongo);
    }

    private static void Map(IEndpointRouteBuilder app, string pattern, ReportSource source) =>
        app.MapGet(pattern, async (
            Guid id,
            IQueryHandler<GetStudentTranscriptQuery, StudentTranscriptResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetStudentTranscriptQuery(source, id);

            Result<StudentTranscriptResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reports);
}
