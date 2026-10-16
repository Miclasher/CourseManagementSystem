using System.Data.Common;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using SharedKernel;

namespace Application.Courses.GetCatalog;

internal sealed class GetCourseCatalogQueryHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetCourseCatalogQuery, List<CatalogItemResponse>>
{
    public async Task<Result<List<CatalogItemResponse>>> Handle(
        GetCourseCatalogQuery query,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<CatalogItemResponse> items = await connection.QueryAsync<CatalogItemResponse>(
            new CommandDefinition(
                """
                SELECT id, title, slug, category_name, lead_instructor_name, price, rating_avg,
                       enrolled_count, lesson_count, total_minutes
                FROM v_course_catalog
                ORDER BY enrolled_count DESC, title
                """,
                cancellationToken: cancellationToken));

        return items.ToList();
    }
}
