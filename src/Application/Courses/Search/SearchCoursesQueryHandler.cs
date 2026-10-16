using System.Data.Common;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using SharedKernel;

namespace Application.Courses.Search;

internal sealed class SearchCoursesQueryHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<SearchCoursesQuery, List<CourseSearchResponse>>
{
    public async Task<Result<List<CourseSearchResponse>>> Handle(
        SearchCoursesQuery query,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<CourseSearchResponse> courses = await connection.QueryAsync<CourseSearchResponse>(
            new CommandDefinition(
                """
                SELECT id, title, slug, level, price, rating_avg, enrolled_count, category_name
                FROM fn_search_courses(@Text::text, @CategoryId::uuid, @PageSize, @Offset)
                """,
                new { query.Text, query.CategoryId, query.PageSize, Offset = (query.Page - 1) * query.PageSize },
                cancellationToken: cancellationToken));

        return courses.ToList();
    }
}
